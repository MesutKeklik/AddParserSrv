using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using SpellChecker.Net.Search.Spell;

namespace AddParserSrv.Classes
{
    public static class Parser
    {
        enum SearchType
        {
            Mah,
            Sok,
            Apt,
            Cad,
            Site,
            Bulv,
            No,
            Kat,
            Daire,
            Blok,
            Bolge
        }

        //sabit regex 
        public const string MahReg = "(( m[ ])|( m[. ])|( mh[ ])|( mh[. ])|( mah[ ])|( mah[. ])|( mahalle.*[ ]))";
        public const string SkReg = "(( s[ ])|( s[. ])|( sk[ ])|( sk[. ])|( sok[ ])|( sok[. ])|( sokak[ ])|( sokağ.*[ ]))";
        public const string AptReg = "(( iş[ ]han.*[ ])|( is[ ]han.*[ ])|( iş[ ]m.*[ ])|( is[ ]m.*[ ])|( bina.*[ ])|( a[ ])|( a[. ])|( ap[ ])|( ap[. ])|( apt[ ])|( apt[. ])|( apart.*[ ])|( p[ ])|( p[. ])|( pl[ ])|( pl[. ])|( plz[ ])|( plz[. ])|( plaz.*[ ])|( i* merkez.*[ ]))";
        public const string CadReg = "(( yol.*[ ])|( c[ ])|( c[. ])|( cd[ ])|( cd[. ])|( cad[ ])|( cad[. ])|( cadde.*[ ]))";
        public const string SiteReg = "(( st[ ])|( st[. ])|( site.*[ ]))";
        public const string BlokReg = "( blok.*[ ])";
        public const string BulvReg = "(( bl[ ])|( bl[. ])|( bulv.*[ ])|( blv.*[ ]))";
        public const string NoReg = "(( n[.])|( n[.:])|( n[:])|( no[.])|( no[.:])|( no[:])|( no[ ]))";
        public const string KatReg = "(( k[.])|( k[.:])|( k[:])|( kat[.])|( kat[.:])|( kat[:])|( kat[ ]))";
        public const string DaireReg = "(( d[.])|( d[.:])|( d[:])|( da[.])|( da[.:])|( da[:])|( daire[:])|( daire[ ]))";
        public const string BolgeReg = "( kamp.*[ ])";


        public static AddressDT ParseAddress(string addressStr)
        {
            addressStr = ChangeTurkishToEnglish(addressStr).ToLower(new CultureInfo("tr-TR"));
            var tmpAddr = addressStr;
            
            //kurala uyan kelimeler
            var rulledMatches = "";
            const string tmpMatch = "";
            var wsSep = new[] { " " };
            var slhSep = new[] { "/" };
            var commaSep = new[] { "," };
            var minusSep = new[] { "-" };
            var reverseSlhSep = new[] { "\\" };

            var dict = GetSearchDict(addressStr);
            var orderedDict = dict.OrderBy(x => x.Key);

            var addr = new AddressDT();
            
            //sıralanmış dictionary arama yapıp addr'nin ilgili alanlarının doldurulduğu kısım.
            foreach (var item in orderedDict)
            {
                var word = FindAddressPart(addressStr, rulledMatches, tmpMatch, item.Value);
                switch (item.Value)
                {
                    case SearchType.Mah:
                        addr.Mahalle = word[0];
                        break;
                    case SearchType.Sok:
                        addr.Sokak = word[0];
                        break;
                    case SearchType.Apt:
                        addr.Bina = word[0];
                        break;
                    case SearchType.Cad:
                        addr.Cadde = word[0];
                        break;
                    case SearchType.Site:
                        addr.Site = word[0];
                        break;
                    case SearchType.Bulv:
                        addr.Bulv = word[0];
                        break;
                    case SearchType.No:
                        addr.No = word[0];
                        break;
                    case SearchType.Kat:
                        addr.Kat = word[0];
                        break;
                    case SearchType.Daire:
                        addr.Daire = word[0];
                        break;
                    case SearchType.Blok:
                        addr.Blok = word[0];
                        break;
                    case SearchType.Bolge:
                        addr.Bolge = word[0];
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                rulledMatches = word[2];
                addressStr = word[1];
            }

            tmpAddr = rulledMatches.Split(wsSep, StringSplitOptions.RemoveEmptyEntries).Select(s => new Regex(Regex.Escape(s))).Aggregate(tmpAddr, (current, regex) => regex.Replace(current, "", 1));

            //kurallara uymayan il ilce semt posta kodu gibi tek kelimelik bilgiler ayiklaniyor
            var cityDistrict = tmpAddr.Split(wsSep, StringSplitOptions.RemoveEmptyEntries);

            //içinde özel karakter ve sayı yoksa il ilçe ya da semttir.
            var cityDistrictFinal = cityDistrict.Where(s => (!s.Contains(".")) && (!s.Contains(":")) && s.All(Char.IsLetter)).ToList();

            foreach (var s in cityDistrict)
            {
                if (s.Contains("/"))
                    cityDistrictFinal.AddRange(s.Split(slhSep, StringSplitOptions.RemoveEmptyEntries));
                if (s.Contains("\\"))
                    cityDistrictFinal.AddRange(s.Split(reverseSlhSep, StringSplitOptions.RemoveEmptyEntries));
                if (s.Contains(","))
                    cityDistrictFinal.AddRange(s.Split(commaSep, StringSplitOptions.RemoveEmptyEntries));
                if (s.Contains("-"))
                    cityDistrictFinal.AddRange(s.Split(minusSep, StringSplitOptions.RemoveEmptyEntries));
            }


            //sadece sayılardan oluşuyorsa ve uzunluğu da 5 ise posta kodudur.
            var postalCode = cityDistrict.Where(s => (!s.Contains(".")) && (!s.Contains(":")) && s.All(Char.IsDigit) && s.Length == 5).ToList();
            addr.PostaKodu = postalCode.Count > 0 ? postalCode[0] : "";

            var cityDistrictFinalDistict = new List<string>(cityDistrictFinal.Select(cityorDistrict => Regex.Replace(cityorDistrict, @"\W|_", "")));

            for (int i = 0; i < cityDistrictFinalDistict.Count; i++)
            {
                cityDistrictFinalDistict[i] = ChangeTurkishToEnglish(cityDistrictFinalDistict[i]);
            }

            cityDistrictFinalDistict = new List<string>(cityDistrictFinalDistict.Distinct());

            var cityDistrictFinalSorted = new List<string>();

            for (var i = cityDistrictFinalDistict.Count-1; i >= 0; i--)
            {
                cityDistrictFinalSorted.Add(cityDistrictFinalDistict[i]);
            }

            var cities = GetCities();
            var counties = GetCounties();
            var districts = GetDistricts();

            while (cityDistrictFinalSorted.Count > 0)
            {
                var sg = SpellCheck(cityDistrictFinalSorted[0], cities, districts, counties);
                cityDistrictFinalSorted.RemoveAt(0);
                if (!sg.IsFound) continue; 

                switch (sg.SuggestedType)
                {
                    case SuggestionType.City:
                        addr.Il = sg.SuggestedWord;
                        counties = GetCounties(addr.Il);
                        break;
                    case SuggestionType.District:
                        addr.Semt = sg.SuggestedWord;
                        break;
                    case SuggestionType.County:
                        addr.Ilce = sg.SuggestedWord;
                        if (addr.Il != null && addr.Ilce != null)
                        districts = GetDistricts(addr.Il, addr.Ilce);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }


            //switch (cityDistrictFinalSorted.Count)
            //{
            //    case 0:
            //        break;
            //    case 1://sadece şehir
            //        addr.Il = SpellCheckCity(cityDistrictFinalSorted[0],GetCities()).SuggestedWord;
            //        break;
            //    case 2://şehir ilçe
            //        addr.Il = SpellCheckCity(cityDistrictFinalSorted[0], GetCities()).SuggestedWord;
            //        addr.Ilce = SpellCheckCounty(cityDistrictFinalSorted[1], GetCounties(addr.Il)).SuggestedWord;
            //        break;
            //    case 3://şehir ilçe semt
            //        addr.Il = SpellCheckCity(cityDistrictFinalSorted[0], GetCities()).SuggestedWord;
            //        addr.Ilce = SpellCheckCounty(cityDistrictFinalSorted[1], GetCounties(addr.Il)).SuggestedWord;
            //        addr.Semt = SpellCheckDistrict(cityDistrictFinalSorted[2], GetDistricts(addr.Ilce, addr.Il)).SuggestedWord;
            //        break;
            //    default: //muhtemel ülke bilgisi de var
            //        var sg = SpellCheckCity(cityDistrictFinalSorted[0], GetCities());
            //        if (!sg.IsFound)
            //            cityDistrictFinalSorted.RemoveAt(0);

            //        addr.Il = SpellCheckCity(cityDistrictFinalSorted[0], GetCities()).SuggestedWord;
            //        addr.Ilce = SpellCheckCounty(cityDistrictFinalSorted[1], GetCounties(addr.Il)).SuggestedWord;
            //        addr.Semt = SpellCheckDistrict(cityDistrictFinalSorted[2], GetDistricts(addr.Ilce, addr.Il)).SuggestedWord;


            //        break;
            //}



            //foreach (var suggestion in cityDistrictFinalSorted.Select(cp => SpellCheck(cp, GetCities(), GetDistricts(), GetCounties())))
            //{
            //    switch (suggestion.SuggestedType)
            //    {
            //        case SuggestionType.City:
            //            addr.Il = suggestion.SuggestedWord;
            //            break;
            //        case SuggestionType.District:
            //            addr.Semt = suggestion.SuggestedWord;
            //            break;
            //        case SuggestionType.County:
            //            addr.Ilce = suggestion.SuggestedWord;
            //            break;
            //        default:
            //            throw new ArgumentOutOfRangeException();
            //    }
            //}

            return addr;
        }

        private static string ChangeTurkishToEnglish(string turkish)
        {
            return
                turkish.Replace("ş", "s")
                       .Replace("ı", "i")
                       .Replace("ö", "o")
                       .Replace("ü", "u")
                       .Replace("ğ", "g")
                       .Replace("ğ", "g")
                       .Replace("ç", "c")
                       .Replace("İ", "i")
                       .Replace("I", "i")
                       .Replace("i̇", "i");
        }

        private static string GetCities()
        {
            var cityStrBuilder = new StringBuilder();

            foreach (var source in Properties.Resources.Cities.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).ToList())
            {
                cityStrBuilder.Append(source.ToLower());
                cityStrBuilder.Append(" ");
            }

            return cityStrBuilder.ToString();
        }

        private static string GetCounties()
        {
            var countyStrBuilder = new StringBuilder();

            foreach (var source in Properties.Resources.Counties.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).ToList())
            {
                countyStrBuilder.Append(source.ToLower());
                countyStrBuilder.Append(" ");
            }

            return countyStrBuilder.ToString();
        }

        private static string GetCounties(string city)
        {
            var x = Properties.Resources.Counties.Split(new[] {"//"}, StringSplitOptions.RemoveEmptyEntries).ToList();
            var countiesOfCity = "";
            foreach (var item in x)
            {
                if (!item.ToLower().Contains(city)) continue;
                countiesOfCity = item.ToLower().Replace(city,"");
                break;
            }


            var countyStrBuilder = new StringBuilder();

            foreach (var source in countiesOfCity.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).ToList()) //Properties.Resources.Counties.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).ToList())
            {
                countyStrBuilder.Append(source.ToLower());
                countyStrBuilder.Append(" ");
            }

            return countyStrBuilder.ToString();
        }

        private static string GetDistricts()
        {
            var distStrBuilder = new StringBuilder();

            foreach (var source in Properties.Resources.Districts.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).ToList())
            {
                distStrBuilder.Append(source.ToLower());
                distStrBuilder.Append(" ");
            }

            return distStrBuilder.ToString();
        }

        private static string GetDistricts(string city, string county)
        {
            var x = Properties.Resources.Districts.Split(new[] { "//" }, StringSplitOptions.RemoveEmptyEntries).ToList();
            var countiesOfCity = "";
            foreach (var item in x.Where(item => item.ToLower().Contains(city)))
            {
                countiesOfCity = item.ToLower().Replace(city, "");
                break;
            }

            x = countiesOfCity.Split(new[] { "&&" }, StringSplitOptions.RemoveEmptyEntries).ToList();
            var districtsOfCounties = "";
            foreach (var item in x)
            {
                if (!item.ToLower().Contains(county)) continue;
                districtsOfCounties = item.ToLower().Replace(county, "");
                break;
            }


            var distStrBuilder = new StringBuilder();

            foreach (var source in districtsOfCounties.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).ToList())
            {
                distStrBuilder.Append(source.ToLower());
                distStrBuilder.Append(" ");
            }

            return distStrBuilder.ToString();
        }

        /// <summary>
        /// tek kelime seklinde girilmis bilgilerin dogrulugunu kontrol edip onerileni getirir
        /// </summary>
        private static SuggestionDT SpellCheck(string word, string cities, string districts, string counties)
        {
            var dir = new RAMDirectory();
            var iw = new IndexWriter(dir, new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_30), IndexWriter.MaxFieldLength.UNLIMITED);

            var distDoc = new Document();
            var textdistField = new Field("text", "", Field.Store.YES, Field.Index.ANALYZED);
            distDoc.Add(textdistField);
            var iddistField = new Field("id", "", Field.Store.YES, Field.Index.NOT_ANALYZED);
            distDoc.Add(iddistField);

            textdistField.SetValue(districts);
            iddistField.SetValue("0");

            var countyDoc = new Document();
            var textcountyField = new Field("text", "", Field.Store.YES, Field.Index.ANALYZED);
            countyDoc.Add(textcountyField);
            var idcountyField = new Field("id", "", Field.Store.YES, Field.Index.NOT_ANALYZED);
            countyDoc.Add(idcountyField);

            textcountyField.SetValue(counties); //İlçe bilgileri bir yerden okunmalı burada elle girilmiş durumda
            idcountyField.SetValue("1");

            var cityDoc = new Document();
            var textcityField = new Field("text", "", Field.Store.YES, Field.Index.ANALYZED);
            cityDoc.Add(textcityField);
            var idcityField = new Field("id", "", Field.Store.YES, Field.Index.NOT_ANALYZED);
            cityDoc.Add(idcityField);

            textcityField.SetValue(cities); //il bilgileri bir yerden okunmalı burada elle girilmiş durumda
            idcityField.SetValue("2");

            iw.AddDocument(distDoc);
            iw.AddDocument(cityDoc);
            iw.AddDocument(countyDoc);

            iw.Commit();
            var reader = iw.GetReader();
            var searcher = new IndexSearcher(reader);
            var retVal = new SuggestionDT();


            var speller = new SpellChecker.Net.Search.Spell.SpellChecker(new RAMDirectory());
            speller.IndexDictionary(new LuceneDictionary(reader, "text"));
            if (speller.Exist(word))
            {
                foreach (var city in (cities.ToLower().Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries)).Where(city => city == word))
                {
                    retVal.SuggestedWord = city;
                    retVal.SuggestedType = SuggestionType.City;
                    break;
                }

                foreach (var county in (counties.ToLower().Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries)).Where(county => county == word))
                {
                    retVal.SuggestedWord = county;
                    retVal.SuggestedType = SuggestionType.County;
                    break;
                }

                foreach (var dist in (districts.ToLower().Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries)).Where(dist => dist == word))
                {
                    retVal.SuggestedWord = dist;
                    retVal.SuggestedType = SuggestionType.District;
                    break;
                }
                retVal.IsFound = true;

            }
            else
            {
                var suggestions = speller.SuggestSimilar(word, 1);
                retVal = new SuggestionDT
                    {
                        SuggestedWord = suggestions.Length > 0 ? suggestions[0] : word,
                        IsFound = suggestions.Length > 0
                    };


                foreach (
                    var doc in
                        suggestions.Select(
                            suggestion =>
                            searcher.Search(new TermQuery(new Term("text", suggestion)), null, Int32.MaxValue))
                                   .SelectMany(docs => docs.ScoreDocs))
                {
                    switch (searcher.Doc(doc.Doc).Get("id"))
                    {
                        case "0":
                            retVal.SuggestedType = SuggestionType.District;
                            break;
                        case "1":
                            retVal.SuggestedType = SuggestionType.County;
                            break;
                        case "2":
                            retVal.SuggestedType = SuggestionType.City;
                            break;
                    }
                }
            }
            reader.Dispose();
            iw.Dispose();

            return retVal;
        }


        /// <summary>
        /// Address belirtilen arama tipine göre gereken kısmı bulur
        /// </summary>
        /// <param name="addressStr"></param>
        /// <param name="rulledMatches"></param>
        /// <param name="tmpMatch"></param>
        /// <param name="sType"></param>
        /// <returns></returns>
        private static string[] FindAddressPart(string addressStr, string rulledMatches, string tmpMatch, SearchType sType)
        {
            int matchIndex = 0;
            bool changeIndex = false;
            bool isNumber = false;
            if (tmpMatch == null) throw new ArgumentNullException("tmpMatch");
            var wsSep = new[] { " " };
            string regularExpression;
            switch (sType)
            {
                case SearchType.Mah:
                    regularExpression = MahReg;
                    break;
                case SearchType.Sok:
                    regularExpression = SkReg;
                    break;
                case SearchType.Apt:
                    regularExpression = AptReg;
                    break;
                case SearchType.Cad:
                    regularExpression = CadReg;
                    break;
                case SearchType.Site:
                    regularExpression = SiteReg;
                    break;
                case SearchType.Bulv:
                    regularExpression = BulvReg;
                    break;
                case SearchType.No:
                    regularExpression = NoReg;
                    changeIndex = true;
                    break;
                case SearchType.Kat:
                    regularExpression = KatReg;
                    isNumber = true;
                    changeIndex = true;
                    break;
                case SearchType.Daire:
                    regularExpression = DaireReg;
                    isNumber = true;
                    changeIndex = true;
                    break;
                case SearchType.Blok:
                    regularExpression = BlokReg;
                    break;
                case SearchType.Bolge:
                    regularExpression = BolgeReg;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("sType");
            }

            var matched = Regex.Matches(addressStr, regularExpression, RegexOptions.IgnoreCase);

            var match = Regex.Split(addressStr, regularExpression, RegexOptions.IgnoreCase);

            if (changeIndex)
                matchIndex = match.Length - 1;


            var repWord = match[matchIndex];

            if (addressStr.Length > repWord.Length)
            {
                if (changeIndex)
                {
                    var replaceWordArray = match[matchIndex].Split(wsSep, StringSplitOptions.RemoveEmptyEntries);
                    repWord = replaceWordArray[0];
                }

                //addressStr = addressStr.Replace(repWord, "");

                if (!isNumber || repWord.Contains("-") || repWord.Contains(".") || repWord.Contains("&") || repWord.Length <= 3)
                {
                    addressStr = ReplaceFirst(addressStr, repWord, "");
                    rulledMatches += " " + repWord;
                }
                else
                {
                    repWord = "";
                }
            }
            else
                repWord = "";
            if (matched.Count > 0)
            {
                if (matched[0].Value.Contains("is") || matched[0].Value.Contains("iş"))
                    tmpMatch = matched[0].Value.Split(' ').Length > 0
                               ? matched[0].Value.Split(wsSep, StringSplitOptions.RemoveEmptyEntries)[0] + " " + matched[0].Value.Split(wsSep, StringSplitOptions.RemoveEmptyEntries)[1] 
                               : matched[0].Value;
                else
                    tmpMatch = matched[0].Value.Split(' ').Length > 0
                                   ? matched[0].Value.Split(wsSep, StringSplitOptions.RemoveEmptyEntries)[0]
                                   : matched[0].Value;
                addressStr = ReplaceFirst(addressStr, tmpMatch, "");
                rulledMatches += " " + tmpMatch;
            }

            return new[] { repWord.Trim(), addressStr, rulledMatches };
        }

        private static string ReplaceFirst(string text, string search, string replace)
        {
            int pos = text.IndexOf(search, StringComparison.Ordinal);
            if (pos < 0)
            {
                return text;
            }
            return text.Substring(0, pos) + replace + text.Substring(pos + search.Length);
        }

        /// <summary>
        /// Adres içinde aramanın yapılacağı tiplerin adres içinde nerede geçtiğini döner
        /// </summary>
        /// <param name="addressStr"></param>
        /// <returns></returns>
        private static Dictionary<int, SearchType> GetSearchDict(string addressStr)
        {
            var matchPos = new Dictionary<int, SearchType>();

            var m = Regex.Matches(addressStr, MahReg, RegexOptions.IgnoreCase);
            var ndx = 0;
            if (m.Count > 0)
                ndx = m[0].Index;

            if (ndx > 0) matchPos.Add(ndx, SearchType.Mah);

            m = Regex.Matches(addressStr, SkReg, RegexOptions.IgnoreCase);
            ndx = 0;
            if (m.Count > 0)
                ndx = m[0].Index;
            if (ndx > 0) matchPos.Add(ndx, SearchType.Sok);

            m = Regex.Matches(addressStr, AptReg, RegexOptions.IgnoreCase);
            ndx = 0;
            if (m.Count > 0)
                ndx = m[0].Index;
            if (ndx > 0) matchPos.Add(ndx, SearchType.Apt);

            m = Regex.Matches(addressStr, CadReg, RegexOptions.IgnoreCase);
            ndx = 0;
            if (m.Count > 0)
                ndx = m[0].Index;
            if (ndx > 0) matchPos.Add(ndx, SearchType.Cad);

            m = Regex.Matches(addressStr, SiteReg, RegexOptions.IgnoreCase);
            ndx = 0;
            if (m.Count > 0)
                ndx = m[0].Index;
            if (ndx > 0) matchPos.Add(ndx, SearchType.Site);

            m = Regex.Matches(addressStr, BulvReg, RegexOptions.IgnoreCase);
            ndx = 0;
            if (m.Count > 0)
                ndx = m[0].Index;
            if (ndx > 0) matchPos.Add(ndx, SearchType.Bulv);

            m = Regex.Matches(addressStr, NoReg, RegexOptions.IgnoreCase);
            ndx = 0;
            if (m.Count > 0)
                ndx = m[0].Index;
            if (ndx > 0) matchPos.Add(ndx, SearchType.No);


            m = Regex.Matches(addressStr, KatReg, RegexOptions.IgnoreCase);
            ndx = 0;
            if (m.Count > 0)
                ndx = m[0].Index;
            if (ndx > 0) matchPos.Add(ndx, SearchType.Kat);


            m = Regex.Matches(addressStr, DaireReg, RegexOptions.IgnoreCase);
            ndx = 0;
            if (m.Count > 0)
                ndx = m[0].Index;
            if (ndx > 0) matchPos.Add(ndx, SearchType.Daire);

            m = Regex.Matches(addressStr, BlokReg, RegexOptions.IgnoreCase);
            ndx = 0;
            if (m.Count > 0)
                ndx = m[0].Index;
            if (ndx > 0) matchPos.Add(ndx, SearchType.Blok);

            m = Regex.Matches(addressStr, BolgeReg, RegexOptions.IgnoreCase);
            ndx = 0;
            if (m.Count > 0)
                ndx = m[0].Index;
            if (ndx > 0) matchPos.Add(ndx, SearchType.Bolge);

            return matchPos;
        }

        //private static SuggestionDT SpellCheckCity(string word, string cities)
        //{
        //    var dir = new RAMDirectory();
        //    var iw = new IndexWriter(dir, new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_30), IndexWriter.MaxFieldLength.UNLIMITED);

        //    var cityDoc = new Document();
        //    var textcityField = new Field("text", "", Field.Store.YES, Field.Index.ANALYZED);
        //    cityDoc.Add(textcityField);
        //    var idcityField = new Field("id", "", Field.Store.YES, Field.Index.NOT_ANALYZED);
        //    cityDoc.Add(idcityField);

        //    textcityField.SetValue(cities); //il bilgileri bir yerden okunmalı burada elle girilmiş durumda
        //    idcityField.SetValue("0");

        //    iw.AddDocument(cityDoc);

        //    iw.Commit();
        //    var reader = iw.GetReader();

        //    var speller = new SpellChecker.Net.Search.Spell.SpellChecker(new RAMDirectory());
        //    speller.IndexDictionary(new LuceneDictionary(reader, "text"));
        //    var suggestions = speller.SuggestSimilar(word, 5);

        //    var retVal = new SuggestionDT { SuggestedWord = suggestions.Length > 0 ? suggestions[0] : word, SuggestedType = SuggestionType.City, IsFound = suggestions.Length > 0 };

        //    reader.Dispose();
        //    iw.Dispose();

        //    return retVal;
        //}

        //private static SuggestionDT SpellCheckCounty(string word, string counties)
        //{
        //    var dir = new RAMDirectory();
        //    var iw = new IndexWriter(dir, new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_30), IndexWriter.MaxFieldLength.UNLIMITED);

        //    var cityDoc = new Document();
        //    var textcityField = new Field("text", "", Field.Store.YES, Field.Index.ANALYZED);
        //    cityDoc.Add(textcityField);
        //    var idcityField = new Field("id", "", Field.Store.YES, Field.Index.NOT_ANALYZED);
        //    cityDoc.Add(idcityField);

        //    textcityField.SetValue(counties); 
        //    idcityField.SetValue("0");

        //    iw.AddDocument(cityDoc);

        //    iw.Commit();
        //    var reader = iw.GetReader();

        //    var speller = new SpellChecker.Net.Search.Spell.SpellChecker(new RAMDirectory());
        //    speller.IndexDictionary(new LuceneDictionary(reader, "text"));
        //    var suggestions = speller.SuggestSimilar(word, 5);

        //    var retVal = new SuggestionDT { SuggestedWord = suggestions.Length > 0 ? suggestions[0] : word, SuggestedType = SuggestionType.County, IsFound = suggestions.Length > 0 };

        //    reader.Dispose();
        //    iw.Dispose();

        //    return retVal;
        //}

        //private static SuggestionDT SpellCheckDistrict(string word, string districts)
        //{
        //    var dir = new RAMDirectory();
        //    var iw = new IndexWriter(dir, new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_30), IndexWriter.MaxFieldLength.UNLIMITED);

        //    var cityDoc = new Document();
        //    var textcityField = new Field("text", "", Field.Store.YES, Field.Index.ANALYZED);
        //    cityDoc.Add(textcityField);
        //    var idcityField = new Field("id", "", Field.Store.YES, Field.Index.NOT_ANALYZED);
        //    cityDoc.Add(idcityField);

        //    textcityField.SetValue(districts);
        //    idcityField.SetValue("0");

        //    iw.AddDocument(cityDoc);

        //    iw.Commit();
        //    var reader = iw.GetReader();

        //    var speller = new SpellChecker.Net.Search.Spell.SpellChecker(new RAMDirectory());
        //    speller.IndexDictionary(new LuceneDictionary(reader, "text"));
        //    var suggestions = speller.SuggestSimilar(word, 5);

        //    var retVal = new SuggestionDT { SuggestedWord = suggestions.Length > 0 ? suggestions[0] : word, SuggestedType = SuggestionType.District, IsFound = suggestions.Length > 0 };

        //    reader.Dispose();
        //    iw.Dispose();

        //    return retVal;
        //}

    }




}
