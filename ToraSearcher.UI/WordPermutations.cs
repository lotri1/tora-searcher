using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToraSearcher.UI
{
    public class WordPermutations
    {
        private static readonly char[] _manzapachEnd = "םןץףך".ToCharArray();
        private static readonly char[] _manzapach = "מנצפכ".ToCharArray();

        public static List<string> GetPermutations(string str)
        {
            var x = str.Length - 1;
            var list = str.ToCharArray();
            var results = new List<string>();

            GetPer(list, 0, x, results);
            results = FixManzapach(results);

            return results;
        }

        private static void GetPer(char[] list, int k, int m, List<string> results)
        {
            if (k == m)
            {
                var str = new string(list);

                if (!results.Contains(str))
                    results.Add(str);
            }
            else
                for (int i = k; i <= m; i++)
                {
                    Swap(ref list[k], ref list[i]);
                    GetPer(list, k + 1, m, results);
                    Swap(ref list[k], ref list[i]);
                }
        }

        private static void Swap(ref char a, ref char b)
        {
            if (a == b) return;

            a ^= b;
            b ^= a;
            a ^= b;
        }

        private static List<string> FixManzapach(List<string> results)
        {
            List<string> newResults = new List<string>();

            foreach (var str in results)
            {
                if (str.Length < 2)
                {
                    newResults.Add(str);
                    continue;
                }
                                
                var newStr = str;

                foreach (var item in _manzapachEnd)
                {
                    var index = newStr.IndexOf(item, 0, str.Length - 1);

                    if (index < 0)
                        continue;

                    newStr = newStr.Remove(index, 1).Insert(index, ((char)(item + 1)).ToString());
                }

                if (_manzapach.Contains(newStr[newStr.Length - 1]))
                {
                    var newLastChar = (char)(newStr[newStr.Length - 1] - 1);
                    newStr = newStr.Remove(newStr.Length - 1, 1).Insert(newStr.Length - 1, newLastChar.ToString());
                }

                newResults.Add(newStr);
            }

            return newResults;
        }
    }
}
