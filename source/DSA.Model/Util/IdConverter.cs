using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DSA.Model.Util
{
    class IdConverter
    {
        public static string ConvertTo18(string id)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            if (id.Length == 18) return id;
            if (id.Length != 15) throw
               new ArgumentException("Illegal argument length. 15 char string expected.", nameof(id));

            var triplet = new List<string> { id.Substring(0, 5),
                                     id.Substring(5, 5),
                                     id.Substring(10, 5) };
            var str = new StringBuilder(5);
            var suffix = string.Empty;
            foreach (var value in triplet)
            {
                str.Clear();
                var reverse = value.Reverse().ToList();
                foreach (char c in reverse)
                {
                    str.Append(Char.IsUpper(c) ? "1" : "0");
                }
                suffix += BinaryIdLookup[str.ToString()];
            }
            return id + suffix;
        }

        static readonly Dictionary<string, char> BinaryIdLookup = new Dictionary<string, char>
        {
            {"00000", 'A'}, {"00001", 'B'}, {"00010", 'C'}, {"00011", 'D'}, {"00100", 'E'},
            {"00101", 'F'}, {"00110", 'G'}, {"00111", 'H'}, {"01000", 'I'}, {"01001", 'J'},
            {"01010", 'K'}, {"01011", 'L'}, {"01100", 'M'}, {"01101", 'N'}, {"01110", 'O'},
            {"01111", 'P'}, {"10000", 'Q'}, {"10001", 'R'}, {"10010", 'S'}, {"10011", 'T'},
            {"10100", 'U'}, {"10101", 'V'}, {"10110", 'W'}, {"10111", 'X'}, {"11000", 'Y'},
            {"11001", 'Z'}, {"11010", '0'}, {"11011", '1'}, {"11100", '2'}, {"11101", '3'},
            {"11110", '4'}, {"11111", '5'}
        };
    }
}
