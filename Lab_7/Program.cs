using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ConsoleApp2
{
    internal class Program
    {
        class Lexeme
        {
            public int lineNum;
            public byte lexNum;
            public string name;
        }
        static void Main()
        {
            Console.WriteLine("Введите имя файла: ");

            string filename = Console.ReadLine();

            var compile = new SyntxAnalize(filename);

            compile.StringAnalize();
            compile.SynthAnalize();
        }
        class SyntxAnalize
        {
            private string file;
            private List<Lexeme> lexemeTable;

            public SyntxAnalize(string file)
            {
                this.file = file;
                this.lexemeTable = new List<Lexeme>();
            }
            private void Error(int lineNum)
            {
                Console.WriteLine($"Ошибка в строке {lineNum}");
            }
            private byte WordAnalize(string s)
            {
                if (s == "DB") return 2;
                if (s == "DW") return 3;
                if (s == "MOV") return 4;
                if (s == "POP") return 5;
                if (s == "IMUL") return 6;
                if (s == "AL" || s == "BL" || s == "CL" || s == "DL" ||
                    s == "AH" || s == "BH" || s == "CH" || s == "DH") return 7;
                if (s == "AX" || s == "BX" || s == "CX" || s == "DX" ||
                    s == "SP" || s == "BP" || s == "SI" || s == "DI") return 8;
                if (s == "ES" || s == "SS" || s == "DS") return 9;
                if (s == "CS") return 10;
                if (int.TryParse(s.TrimEnd('H'), System.Globalization.NumberStyles.HexNumber, null, out _)) return 11;
                return 12;
            }
            public void StringAnalize()
            {
                var lines = File.ReadLines(this.file);
                int lineNum = 0;

                foreach (var line in lines)
                {
                    lineNum++;
                    int symbolNumber = 0;
                    var words = line.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (var word in words)
                    {
                        symbolNumber += word.Length;
                        string processedWord = word.ToUpper();

                        byte lexemeNumber = WordAnalize(processedWord);
                        string lexemeValue = lexemeNumber > 6 ? processedWord : string.Empty;

                        Lexeme lexeme = new Lexeme { lineNum = lineNum, lexNum = lexemeNumber, name = lexemeValue };

                        lexemeTable.Add(lexeme);

                        if (line.Contains(","))
                        {
                            Lexeme commaLexeme = new Lexeme { lineNum = lineNum, lexNum = WordAnalize(","), name = string.Empty };
                            lexemeTable.Add(commaLexeme);
                        }
                        symbolNumber++;
                    }
                }

            }
            private bool IsNumber(int i, int size)
            {
                int n;
                bool isHex = int.TryParse(lexemeTable[i].name.TrimEnd('H'), System.Globalization.NumberStyles.HexNumber, null, out n);
                bool isDec = int.TryParse(lexemeTable[i].name, out n);
                return (isHex || isDec) && ((size == 1 && n < 256) || (size == 2 && n < 65536));
            }

            private bool IsDefinedAsWord(int i)
            {
                for (int j = 0; j < i; j++)
                    if (lexemeTable[j].name == lexemeTable[i].name && lexemeTable[j + 1].lexNum == 3)
                        return true;

                return false;
            }
            private bool Number(string word, out int value)
            {
                if (word.EndsWith("H"))
                {
                    if (int.TryParse(word.Substring(0, word.Length - 1), System.Globalization.NumberStyles.HexNumber, null, out value))
                        return true;
                    else
                    {
                        value = 0;
                        return false;
                    }
                }
                else
                {
                    if (int.TryParse(word, out value))
                        return true;
                    else
                    {
                        value = 0;
                        return false;
                    }
                }
            }
            public void SynthAnalize()
            {
                int firstIndex = 0; 
                int lastIndex = 0;

                for (int i = 0; i < lexemeTable[lexemeTable.Count - 1].lineNum - 1; i++)
                {
                    for (int j = firstIndex; j < lexemeTable.Count - 1; j++)
                    {
                        if (lexemeTable[firstIndex].lineNum != lexemeTable[j + 1].lineNum) 
                            break;
                        lastIndex++;
                    }

                    if (lexemeTable[firstIndex].lexNum == 0)
                    {
                        Error(lexemeTable[firstIndex].lineNum);
                        return;
                    }
                    else if (lexemeTable[firstIndex].lexNum == 4) //MOV
                    {
                        if (i + 3 < lexemeTable.Count &&
                            lexemeTable[i].lexNum == 4 &&
                            lexemeTable[i + 1].lexNum == 8 &&
                            lexemeTable[i + 2].lexNum == 1 &&
                            (lexemeTable[i + 3].lexNum == 9 ||
                            lexemeTable[i + 3].lexNum == 12 && IsDefinedAsWord(i + 3) ||
                            lexemeTable[i + 3].lexNum == 8 ||
                            lexemeTable[i + 3].lexNum == 11 && IsNumber(i + 3, 2)))
                        {
                            Error(lexemeTable[firstIndex].lineNum);
                            return;
                        }
                        if (lastIndex - firstIndex != 5)
                        {
                            Error(lexemeTable[firstIndex].lineNum);
                            Console.WriteLine("MOV принимает два операнда");
                            return;
                        }
                    }
                    else if (lexemeTable[firstIndex].lexNum == 5) //POP
                    {
                        if (i + 1 < lexemeTable.Count &&
                            lexemeTable[i].lexNum == 5 &&
                            (lexemeTable[i + 1].lexNum == 8 ||
                            lexemeTable[i + 1].lexNum == 9 ||
                            lexemeTable[i + 1].lexNum == 12 && IsDefinedAsWord(i + 1)))
                        {
                            Error(lexemeTable[firstIndex].lineNum);
                            return;
                        }
                        if (lastIndex - firstIndex != 1)
                        {
                            Error(lexemeTable[firstIndex].lineNum);
                            Console.WriteLine("POP принимает один операнд");
                            return;
                        }
                    }
                    else if (lexemeTable[firstIndex].lexNum == 6) //IMUL
                    {
                        if (i + 1 < lexemeTable.Count &&
                            lexemeTable[i].lexNum == 6 &&
                            (lexemeTable[i + 1].lexNum == 8 ||
                            lexemeTable[i + 1].lexNum == 9 ||
                            lexemeTable[i + 1].lexNum == 12 && IsDefinedAsWord(i + 1)))
                        {
                            Error(lexemeTable[firstIndex].lineNum);
                            return;
                        }
                        if (lastIndex - firstIndex != 1)
                        {
                            Error(lexemeTable[firstIndex].lineNum);
                            Console.WriteLine("IMUL принимает один операнд");
                            return;
                        }
                        if (lastIndex - firstIndex == 3)
                        {
                            Error(lexemeTable[firstIndex].lineNum);
                            return;
                        }
                    }
                    else if (lexemeTable[firstIndex].lexNum == 12)
                    {
                        var lexNum = lexemeTable[firstIndex + 1].lexNum;

                        if (lexNum != WordAnalize("DB") && lexNum != WordAnalize("DW"))
                        {
                            Error(lexemeTable[firstIndex].lineNum);
                            return;
                        }

                        for (int k = firstIndex + 2; k < lastIndex; k++)
                        {
                            if (!Number(lexemeTable[k].name, out int number) || !IsValidNumber(lexNum, number))
                            {
                                Error(lexemeTable[firstIndex].lineNum);
                                return;
                            }
                        }
                    }
                    else
                    {
                        Error(lexemeTable[firstIndex].lineNum);
                        return;
                    }

                    lastIndex++;
                    firstIndex = lastIndex;
                }

                Console.WriteLine("Ошибок нет!");

                return;
            }
            bool IsValidNumber(byte lexNum, int number)
            {
                return (lexNum == WordAnalize("DB") && number > -129 && number < 257) ||
                       (lexNum == WordAnalize("DW") && number > -32768 && number < 65537);
            }
        }
    }
}
