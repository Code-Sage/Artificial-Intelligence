using System;
using System.Text;
using System.IO;

namespace Sentiment_Analysis_Using_Naive_Bayes
{
	/*

        Porter stemmer in CSharp, based on the Java port. The original paper is in

            Porter, 1980, An algorithm for suffix stripping, Program, Vol. 14,
            no. 3, pp 130-137,

        See also http://www.tartarus.org/~martin/PorterStemmer

        History:

        Release 1

        Bug 1 (reported by Gonzalo Parra 16/10/99) fixed as marked below.
        The words 'aed', 'eed', 'oed' leave k at 'a' for step 3, and b[k-1]
        is then out outside the bounds of b.

        Release 2

        Similarly,

        Bug 2 (reported by Steve Dyrdahl 22/2/00) fixed as marked below.
        'ion' by itself leaves j = -1 in the test for 'ion' in step 5, and
        b[j] is then outside the bounds of b.

        Release 3

        Considerably revised 4/9/00 in the light of many helpful suggestions
        from Brian Goetz of Quiotix Corporation (brian@quiotix.com).

        Release 4

     */

    /**
      * Stemmer, implementing the Porter Stemming Algorithm
      *
      * The Stemmer class transforms a word into its root form.  The input
      * word can be provided a character at time (by calling add()), or at once
      * by calling one of the various stem(something) methods.
      */

    class Stemmer
    {
        private char[] b;
        private int i,     /* offset into b */
            i_end, /* offset to end of stemmed word */
            j, k;
        private static int INC = 50;
        /* unit of size whereby b is increased */

        public Stemmer()
        {
            b = new char[INC];
            i = 0;
            i_end = 0;
        }

        /**
         * Add a character to the word being stemmed.  When you are finished
         * adding characters, you can call stem(void) to stem the word.
         */

        public void add(char ch)
        {
            if (i == b.Length)
            {
                char[] new_b = new char[i + INC];
                for (int c = 0; c < i; c++)
                    new_b[c] = b[c];
                b = new_b;
            }
            b[i++] = ch;
        }


        /** Adds wLen characters to the word being stemmed contained in a portion
         * of a char[] array. This is like repeated calls of add(char ch), but
         * faster.
         */

        public void add(char[] w, int wLen)
        {
            if (i + wLen >= b.Length)
            {
                char[] new_b = new char[i + wLen + INC];
                for (int c = 0; c < i; c++)
                    new_b[c] = b[c];
                b = new_b;
            }
            for (int c = 0; c < wLen; c++)
                b[i++] = w[c];
        }

        /**
         * After a word has been stemmed, it can be retrieved by toString(),
         * or a reference to the internal buffer can be retrieved by getResultBuffer
         * and getResultLength (which is generally more efficient.)
         */
        public override string ToString()
        {
            return new String(b, 0, i_end);
        }

        /**
         * Returns the length of the word resulting from the stemming process.
         */
        public int getResultLength()
        {
            return i_end;
        }

        /**
         * Returns a reference to a character buffer containing the results of
         * the stemming process.  You also need to consult getResultLength()
         * to determine the length of the result.
         */
        public char[] getResultBuffer()
        {
            return b;
        }

        /* cons(i) is true <=> b[i] is a consonant. */
        private bool cons(int i)
        {
            switch (b[i])
            {
                case 'a':
                case 'e':
                case 'i':
                case 'o':
                case 'u': return false;
                case 'y': return (i == 0) ? true : !cons(i - 1);
                default: return true;
            }
        }

        /* m() measures the number of consonant sequences between 0 and j. if c is
           a consonant sequence and v a vowel sequence, and <..> indicates arbitrary
           presence,

              <c><v>       gives 0
              <c>vc<v>     gives 1
              <c>vcvc<v>   gives 2
              <c>vcvcvc<v> gives 3
              ....
        */
        private int m()
        {
            int n = 0;
            int i = 0;
            while (true)
            {
                if (i > j) return n;
                if (!cons(i)) break; i++;
            }
            i++;
            while (true)
            {
                while (true)
                {
                    if (i > j) return n;
                    if (cons(i)) break;
                    i++;
                }
                i++;
                n++;
                while (true)
                {
                    if (i > j) return n;
                    if (!cons(i)) break;
                    i++;
                }
                i++;
            }
        }

        /* vowelinstem() is true <=> 0,...j contains a vowel */
        private bool vowelinstem()
        {
            int i;
            for (i = 0; i <= j; i++)
                if (!cons(i))
                    return true;
            return false;
        }

        /* doublec(j) is true <=> j,(j-1) contain a double consonant. */
        private bool doublec(int j)
        {
            if (j < 1)
                return false;
            if (b[j] != b[j - 1])
                return false;
            return cons(j);
        }

        /* cvc(i) is true <=> i-2,i-1,i has the form consonant - vowel - consonant
           and also if the second c is not w,x or y. this is used when trying to
           restore an e at the end of a short word. e.g.

              cav(e), lov(e), hop(e), crim(e), but
              snow, box, tray.

        */
        private bool cvc(int i)
        {
            if (i < 2 || !cons(i) || cons(i - 1) || !cons(i - 2))
                return false;
            int ch = b[i];
            if (ch == 'w' || ch == 'x' || ch == 'y')
                return false;
            return true;
        }

        private bool ends(String s)
        {
            int l = s.Length;
            int o = k - l + 1;
            if (o < 0)
                return false;
            char[] sc = s.ToCharArray();
            for (int i = 0; i < l; i++)
                if (b[o + i] != sc[i])
                    return false;
            j = k - l;
            return true;
        }

        /* setto(s) sets (j+1),...k to the characters in the string s, readjusting
           k. */
        private void setto(String s)
        {
            int l = s.Length;
            int o = j + 1;
            char[] sc = s.ToCharArray();
            for (int i = 0; i < l; i++)
                b[o + i] = sc[i];
            k = j + l;
        }

        /* r(s) is used further down. */
        private void r(String s)
        {
            if (m() > 0)
                setto(s);
        }

        /* step1() gets rid of plurals and -ed or -ing. e.g.
               caresses  ->  caress
               ponies    ->  poni
               ties      ->  ti
               caress    ->  caress
               cats      ->  cat

               feed      ->  feed
               agreed    ->  agree
               disabled  ->  disable

               matting   ->  mat
               mating    ->  mate
               meeting   ->  meet
               milling   ->  mill
               messing   ->  mess

               meetings  ->  meet

        */

        private void step1()
        {
            if (b[k] == 's')
            {
                if (ends("sses"))
                    k -= 2;
                else if (ends("ies"))
                    setto("i");
                else if (b[k - 1] != 's')
                    k--;
            }
            if (ends("eed"))
            {
                if (m() > 0)
                    k--;
            }
            else if ((ends("ed") || ends("ing")) && vowelinstem())
            {
                k = j;
                if (ends("at"))
                    setto("ate");
                else if (ends("bl"))
                    setto("ble");
                else if (ends("iz"))
                    setto("ize");
                else if (doublec(k))
                {
                    k--;
                    int ch = b[k];
                    if (ch == 'l' || ch == 's' || ch == 'z')
                        k++;
                }
                else if (m() == 1 && cvc(k)) setto("e");
            }
        }

        /* step2() turns terminal y to i when there is another vowel in the stem. */
        private void step2()
        {
            if (ends("y") && vowelinstem())
                b[k] = 'i';
        }

        /* step3() maps double suffices to single ones. so -ization ( = -ize plus
           -ation) maps to -ize etc. note that the string before the suffix must give
           m() > 0. */
        private void step3()
        {
            if (k == 0)
                return;

            /* For Bug 1 */
            switch (b[k - 1])
            {
                case 'a':
                    if (ends("ational")) { r("ate"); break; }
                    if (ends("tional")) { r("tion"); break; }
                    break;
                case 'c':
                    if (ends("enci")) { r("ence"); break; }
                    if (ends("anci")) { r("ance"); break; }
                    break;
                case 'e':
                    if (ends("izer")) { r("ize"); break; }
                    break;
                case 'l':
                    if (ends("bli")) { r("ble"); break; }
                    if (ends("alli")) { r("al"); break; }
                    if (ends("entli")) { r("ent"); break; }
                    if (ends("eli")) { r("e"); break; }
                    if (ends("ousli")) { r("ous"); break; }
                    break;
                case 'o':
                    if (ends("ization")) { r("ize"); break; }
                    if (ends("ation")) { r("ate"); break; }
                    if (ends("ator")) { r("ate"); break; }
                    break;
                case 's':
                    if (ends("alism")) { r("al"); break; }
                    if (ends("iveness")) { r("ive"); break; }
                    if (ends("fulness")) { r("ful"); break; }
                    if (ends("ousness")) { r("ous"); break; }
                    break;
                case 't':
                    if (ends("aliti")) { r("al"); break; }
                    if (ends("iviti")) { r("ive"); break; }
                    if (ends("biliti")) { r("ble"); break; }
                    break;
                case 'g':
                    if (ends("logi")) { r("log"); break; }
                    break;
                default:
                    break;
            }
        }

        /* step4() deals with -ic-, -full, -ness etc. similar strategy to step3. */
        private void step4()
        {
            switch (b[k])
            {
                case 'e':
                    if (ends("icate")) { r("ic"); break; }
                    if (ends("ative")) { r(""); break; }
                    if (ends("alize")) { r("al"); break; }
                    break;
                case 'i':
                    if (ends("iciti")) { r("ic"); break; }
                    break;
                case 'l':
                    if (ends("ical")) { r("ic"); break; }
                    if (ends("ful")) { r(""); break; }
                    break;
                case 's':
                    if (ends("ness")) { r(""); break; }
                    break;
            }
        }

        /* step5() takes off -ant, -ence etc., in context <c>vcvc<v>. */
        private void step5()
        {
            if (k == 0)
                return;

            /* for Bug 1 */
            switch (b[k - 1])
            {
                case 'a':
                    if (ends("al")) break; return;
                case 'c':
                    if (ends("ance")) break;
                    if (ends("ence")) break; return;
                case 'e':
                    if (ends("er")) break; return;
                case 'i':
                    if (ends("ic")) break; return;
                case 'l':
                    if (ends("able")) break;
                    if (ends("ible")) break; return;
                case 'n':
                    if (ends("ant")) break;
                    if (ends("ement")) break;
                    if (ends("ment")) break;
                    /* element etc. not stripped before the m */
                    if (ends("ent")) break; return;
                case 'o':
                    if (ends("ion") && j >= 0 && (b[j] == 's' || b[j] == 't')) break;
                    /* j >= 0 fixes Bug 2 */
                    if (ends("ou")) break; return;
                /* takes care of -ous */
                case 's':
                    if (ends("ism")) break; return;
                case 't':
                    if (ends("ate")) break;
                    if (ends("iti")) break; return;
                case 'u':
                    if (ends("ous")) break; return;
                case 'v':
                    if (ends("ive")) break; return;
                case 'z':
                    if (ends("ize")) break; return;
                default:
                    return;
            }
            if (m() > 1)
                k = j;
        }

        /* step6() removes a final -e if m() > 1. */
        private void step6()
        {
            j = k;

            if (b[k] == 'e')
            {
                int a = m();
                if (a > 1 || a == 1 && !cvc(k - 1))
                    k--;
            }
            if (b[k] == 'l' && doublec(k) && m() > 1)
                k--;
        }

        /** Stem the word placed into the Stemmer buffer through calls to add().
         * Returns true if the stemming process resulted in a word different
         * from the input.  You can retrieve the result with
         * getResultLength()/getResultBuffer() or toString().
         */
        public void stem()
        {
            k = i - 1;
            if (k > 1)
            {
                step1();
                step2();
                step3();
                step4();
                step5();
                step6();
            }
            i_end = k + 1;
            i = 0;
        }

        /** Test program for demonstrating the Stemmer.  It reads text from a
         * a list of files, stems each word, and writes the result to standard
         * output. Note that the word stemmed is expected to be in lower case:
         * forcing lower case must be done outside the Stemmer class.
         * Usage: Stemmer file-name file-name ...
         */
    //    public static void Main(String[] args)
    //    {
    //        if (args.Length == 0)
    //        {
    //            Console.WriteLine("Usage:  Stemmer <input file>");
    //            return;
    //        }
    //        char[] w = new char[501];
    //        Stemmer s = new Stemmer();
            //for (int i = 0; i < args.Length; i++)
            //{
            //    try
            //    {
            //        FileStream _in = new FileStream(args[i], FileMode.Open, FileAccess.Read);
            //        try
            //        {
            //            while (true)
            //            {
            //                int ch = _in.ReadByte();
            //                if (Char.IsLetter((char)ch))
            //                {
            //                    int j = 0;
            //                    while (true)
            //                    {
            //                        ch = Char.ToLower((char)ch);
            //                        w[j] = (char)ch;
            //                        if (j < 500)
            //                            j++;
            //                        ch = _in.ReadByte();
            //                        if (!Char.IsLetter((char)ch))
            //                        {
            //                            /* to test add(char ch) */
            //                            for (int c = 0; c < j; c++)
            //                                s.add(w[c]);
            //                            /* or, to test add(char[] w, int j) */
            //                            /* s.add(w, j); */
            //                            s.stem();

            //                            String u;

            //                            /* and now, to test toString() : */
            //                            u = s.ToString();

            //                            /* to test getResultBuffer(), getResultLength() : */
            //                            /* u = new String(s.getResultBuffer(), 0, s.getResultLength()); */

            //                            Console.Write(u);
            //                            break;
            //                        }
            //                    }
            //                }
            //                if (ch < 0)
            //                    break;
            //                Console.Write((char)ch);
            //            }
            //        }
            //        catch (IOException)
            //        {
            //            Console.WriteLine("error reading " + args[i]);
            //            break;
            //        }
            //    }
            //    catch (FileNotFoundException)
            //    {
            //        Console.WriteLine("file " + args[i] + " not found");
            //        break;
            //    }
            //}
       //}
    }


	class List<T>
	{
		int count, maxSize;
		T[] items;

		public List()
		{
			count = 0;
			items = new T[maxSize = 100];
		}

		public void Add(T item)
		{
			if (count >= maxSize)
			{
				T[] temp = new T[maxSize * 2];
				for (int i = 0; i < count; ++i)
					temp[i] = items[i];
				maxSize *= 2;
				items = temp;
			}
			items[count++] = item;
		}

		public int Length { get { return count; } }

		public void Clear() { count = 0; }

		public T[] ToArray()
		{
			T[] temp = new T[count];
			for (int i = 0; i < count; ++i)
				temp[i] = items[i];
			return temp;
		}

		public void SetValue(T Value)
		{
			for (int i = 0; i < count; ++i)
				items[i] = Value;
		}

		public T this[int i] { get { return items[i]; } set { items[i] = value; } }
	}

	class Program
	{
		static List<int[]> frequencies = new List<int[]>();
		static List<bool> found = new List<bool>();
		static List<double[]> probabilities = new List<double[]>();
		static List<string> sorted_on_termStrs = new List<string>(), sorted_on_IDs = new List<string>();
		static List<int> indices = new List<int>(), calculatedLabels = new List<int>(), actualLabels = new List<int>();
		static int[] classToIndex = { -1, 0, 1, -1, 2, 3}, indexToClass = { 1, 2, 4, 5 }, freqSumOfClass = {0, 0, 0, 0};
		static bool[] isClassSumAvailable = { false, false, false, false};
		static string[] stopWords = LoadStopWords(), directories = Directory.GetDirectories("sorted_data"), training_files = { "negative.review", "positive.review" }, picking_files = { "unlabeled.review", "all.review" }, picked_files = {@"\Picked_unlabeled.review", @"\Picked.review" };
		static double[] priors = {0, 0, 0, 0};

		#region Basic API

		public static int GetIndexIfPresent(List<string> items, string itemToFind)
		{
			int i = 0, j = items.Length - 1, m, compResult;
			while (i <= j)
			{
				m = (i + j) / 2;
				compResult = items[m].CompareTo(itemToFind);
				if (compResult < 0)
					i = m + 1;
				else if (compResult > 0)
					j = m - 1;
				else
					return m;
			}
			return -1;
		}

		public static int GetProbableIndex(List<string> items, string itemToInsert)
		{
			int i = 0, j = items.Length - 1, m, compResult;
			while (i <= j)
			{
				m = (i + j) / 2;
				compResult = items[m].CompareTo(itemToInsert);
				if (compResult < 0)
					i = m + 1;
				else if (compResult > 0)
					j = m - 1;
			}
			return i;
		}

		static string[] LoadStopWords()
		{
			List<string> words = new List<string>();
			StreamReader reader = new StreamReader(new FileStream(@"sorted_data/stopwords", FileMode.Open));
			string input;
			while ((input = reader.ReadLine()) != null)
				words.Add(input);
			reader.Close();
			return words.ToArray();
		}

		static bool IsPresentInList(string[] wordList, string word)
		{
			int i = 0, j = wordList.Length - 1, m, compResult;
			while (i <= j)
			{
				m = (i + j) / 2;
				compResult = wordList[m].CompareTo(word);
				if (compResult < 0)
					i = m + 1;
				else if (compResult > 0)
					j = m - 1;
				else
					return true;
			}
			return false;
		}

		static bool IsPresentInList(List<string> wordList, string word)
		{
			int i = 0, j = wordList.Length - 1, m, compResult;
			while (i <= j)
			{
				m = (i + j) / 2;
				compResult = wordList[m].CompareTo(word);
				if (compResult < 0)
					i = m + 1;
				else if (compResult > 0)
					j = m - 1;
				else
					return true;
			}
			return false;
		}

		#endregion

		#region Text Operations

		#region Train

		static void Parse()
		{
			StreamReader reader;
			string input, rating = "";
			StreamWriter[] writers = new StreamWriter[4];
			int i = 0, ratingInt;
			StringBuilder reviewTextBuilder = new StringBuilder("");
			foreach (string category in directories)
			{
				for (int j = 0; j < writers.Length; ++j)
					writers[j] = new StreamWriter(new FileStream(category + @"/" + indexToClass[j] + ".review", FileMode.Create));
				for(int k = 0; k < 2; ++k)
				{
					reader = new StreamReader(new FileStream(category + @"/" + training_files[k], FileMode.Open));
					while (((input = reader.ReadLine()) != null))
						if (input.Contains("<review>"))
						{
							input = reader.ReadLine();
							while (!(input.Contains("</review>")))
							{
								input = reader.ReadLine();
								if (input.Contains("<rating>"))
									rating = input = reader.ReadLine();
								else if (input.Contains("<review_text>"))
								{
									input = reader.ReadLine();
									reviewTextBuilder.Append(input).Append(' ');
									while (!((input = reader.ReadLine()).Contains("</review_text>")))
										reviewTextBuilder.Append(' ').Append(input);
									ratingInt = (int)Convert.ToDouble(rating);
									writers[classToIndex[ratingInt]].WriteLine(reviewTextBuilder.ToString());
									reviewTextBuilder.Clear();
									++i;
									if (i % 200 == 0)
										writers[classToIndex[ratingInt]].Flush();
									Console.WriteLine(i + 1);
									if(i == 1000)
										i = i;
								}
							}
						}
					reader.Close();
					writers[k * 2].Close();
					writers[k * 2 + 1].Close();
				}
			}
		}

		static void OmitStopWords()
		{
			StreamReader reader;
			string input, review_text = "";
			StreamWriter writer;
			int j = 0;
			StringBuilder builder = new StringBuilder("");
			foreach (string category in directories)
				foreach(int index in indexToClass)
				{
					reader = new StreamReader(new FileStream(category + @"/" + index + ".review", FileMode.Open));
					writer = new StreamWriter(new FileStream(category + @"/temp" + index + ".review", FileMode.Create));
					while ((input = reader.ReadLine()) != null)
					{
						review_text = input;
						string[] tokens = review_text.Split(' ', ',', '"', ':', ';', '.', '\t', '\n', '!', '-', '?', '@', '%', '(', ')', '`', '/', '=', '\\', '~', '#', '$', '%', '^', '&', '*', '_', '|', '[', ']', '{', '}', '<', '>', '+');
						if (tokens != null)
							for (int i = 0; i < tokens.Length; ++i)
								if (!string.IsNullOrWhiteSpace(tokens[i]))
								{
									tokens[i] = tokens[i].ToLower();
									if (!IsPresentInList(stopWords, tokens[i]))
										builder.Append(tokens[i]).Append(' ');
								}
						writer.WriteLine(builder.ToString());
						builder.Clear();
						if (j % 50 == 0)
							writer.Flush();
						++j;
					}
					writer.Close();
					reader.Close();
					File.Copy(category + @"/temp" + index + ".review", category + @"/" + index + ".review", true);
					File.Delete(category + @"/temp" + index + ".review");
				}
		}

		static void StemDataset()
		{
			Stemmer stemmer;
			StreamReader reader;
			string input, review_text = "";
			StreamWriter writer;
			StringBuilder builder = new StringBuilder("");
			int j = 0;	
			foreach (string category in directories)
				foreach(int index in indexToClass)
				{
					reader = new StreamReader(new FileStream(category + @"/" + index + ".review", FileMode.Open));
					writer = new StreamWriter(new FileStream(category + @"/temp" + index + ".review", FileMode.Create));
					while ((input = reader.ReadLine()) != null)
					{
						review_text = input;
						string[] tokens = review_text.Split(' ', ',', '"', ':', ';', '.', '\t', '\n', '!', '-', '?', '@', '%', '(', ')', '`', '/', '=', '\\', '~', '#', '$', '%', '^', '&', '*', '_', '|', '[', ']', '{', '}', '<', '>', '+');
						for (int i = 0; i < tokens.Length; ++i)
							if (!string.IsNullOrWhiteSpace(tokens[i]))
							{
								stemmer = new Stemmer();
								tokens[i] = tokens[i].Replace("'", "").Replace("(", "").Replace(")", "").Replace("[", "").Replace("]", "").Replace("{", "").Replace("}", "");
								char[] toAdd = tokens[i].ToCharArray();
								stemmer.add(toAdd, toAdd.Length);
								stemmer.stem();
								builder.Append(stemmer.ToString()).Append(' ');
							}
						writer.WriteLine(builder.ToString());
						builder.Clear();
						if (j % 50 == 0)
							writer.Flush();
						++j;
					}
					writer.Close();
					reader.Close();
					File.Copy(category + @"/temp" + index + ".review", category + @"/" + index + ".review", true);
					File.Delete(category + @"/temp" + index + ".review");
				}
		}

		#endregion

		#region Test

		static void Parse_for_Test()
		{
			StreamReader reader;
			string input;
			StreamWriter[] writers = new StreamWriter[4];
			int count, ratingInt;
			StringBuilder reviewTextBuilder = new StringBuilder("");
			foreach (string category in directories)
			{
				count = 0;
				reader = new StreamReader(new FileStream(category + @"/TestSet.review", FileMode.Open));
				for (int k = 0; k < 4; ++k)
					writers[k] = new StreamWriter(new FileStream(category + @"/" + indexToClass[k] + "_Test.review", FileMode.Create));
				while (count < 100000 && ((input = reader.ReadLine()) != null))
				{
					ratingInt = (int)Convert.ToDouble(input);	// input == rating
					reviewTextBuilder.Append(reader.ReadLine());
					writers[classToIndex[ratingInt]].WriteLine(reviewTextBuilder.ToString());
					reviewTextBuilder.Clear();
					++count;
					if (count % 200 == 0)
						writers[classToIndex[ratingInt]].Flush();
					Console.WriteLine(count + 1);
				}
				reader.Close();
				for (int k = 0; k < 4; ++k)
					writers[k].Close();
			}
		}

		static void OmitStopWords_for_test()
		{
			StreamReader reader;
			string review_text = "";
			StreamWriter writer;
			int j = 0;
			StringBuilder builder = new StringBuilder("");
			foreach (string category in directories)
				foreach (int index in indexToClass)
				{
					reader = new StreamReader(new FileStream(category + @"/" + index + "_Test.review", FileMode.Open));
					writer = new StreamWriter(new FileStream(category + @"/temp" + index + "_Test.review", FileMode.Create));
					while ((review_text = reader.ReadLine()) != null)
					{
						string[] tokens = review_text.Split(' ', ',', '"', ':', ';', '.', '\t', '\n', '!', '-', '?', '@', '%', '(', ')', '`', '/', '=', '\\', '~', '#', '$', '%', '^', '&', '*', '_', '|', '[', ']', '{', '}', '<', '>', '+');
						if (tokens != null)
							for (int i = 0; i < tokens.Length; ++i)
								if (!string.IsNullOrWhiteSpace(tokens[i]))
								{
									tokens[i] = tokens[i].ToLower();
									if (!IsPresentInList(stopWords, tokens[i]))
										builder.Append(tokens[i]).Append(' ');
								}
						writer.WriteLine(builder.ToString());
						builder.Clear();
						if (j % 50 == 0)
							writer.Flush();
						++j;
					}
					writer.Close();
					reader.Close();
					File.Copy(category + @"/temp" + index + "_Test.review", category + @"/" + index + "_Test.review", true);
					File.Delete(category + @"/temp" + index + "_Test.review");
				}
		}

		static void StemDataSet_for_Test()
		{
			Stemmer stemmer;
			StreamReader reader;
			string review_text = "";
			StreamWriter writer;
			StringBuilder builder = new StringBuilder("");
			int j = 0;
			foreach (string category in directories)
				foreach(int index in indexToClass)
				{
					reader = new StreamReader(new FileStream(category + @"/" + index + "_Test.review", FileMode.Open));
					writer = new StreamWriter(new FileStream(category + @"/temp" + index + "_Test.review", FileMode.Create));
					while ((review_text = reader.ReadLine()) != null)
					{
						string[] tokens = review_text.Split(' ', ',', '"', ':', ';', '.', '\t', '\n', '!', '-', '?', '@', '%', '(', ')', '`', '/', '=', '\\', '~', '#', '$', '%', '^', '&', '*', '_', '|', '[', ']', '{', '}', '<', '>', '+');
						for (int i = 0; i < tokens.Length; ++i)
							if (!string.IsNullOrWhiteSpace(tokens[i]))
							{
								stemmer = new Stemmer();
								tokens[i] = tokens[i].Replace("'", "").Replace("(", "").Replace(")", "").Replace("[", "").Replace("]", "").Replace("{", "").Replace("}", "");
								char[] toAdd = tokens[i].ToCharArray();
								stemmer.add(toAdd, toAdd.Length);
								stemmer.stem();
								builder.Append(stemmer.ToString()).Append(' ');
							}
						writer.WriteLine(builder.ToString());
						builder.Clear();
						if (j % 50 == 0)
							writer.Flush();
						++j;
					}
					writer.Close();
					reader.Close();
					File.Copy(category + @"/temp" + index + "_Test.review", category + @"/" + index + "_Test.review", true);
					File.Delete(category + @"/temp" + index + "_Test.review");

				}
		}

		static List<string> GetVocabulary(string[] tokens)
		{
			List<string> list = new List<string>();
			int index;
			foreach(string token in tokens)
				if(GetIndexIfPresent(list, token) < 0)
				{
					index = GetProbableIndex(list, token);
					for (int i = list.Length - 1; i > index; --i)
						list[i] = list[i - 1];
					list[index] = token;
				}
			return list;
		}

		#endregion

		#endregion

		#region Multinomial API

		public static void InsertIntoVocabulary_Multinomial(List<string> onTerms, List<int> indices, List<string> onIDs, List<int[]> frequencies, string itemToInsert, int insertionIndex, int classIndex)
		{
			onIDs.Add(itemToInsert);
			int[] freqs = new int[4];
			for (int i = 0; i < 4; ++i)
				if (i == classIndex)
					freqs[i] = 1;
				else
					freqs[i] = 0;
			frequencies.Add(freqs);
			onTerms.Add(itemToInsert);
			indices.Add(onTerms.Length - 1);
			for (int i = onTerms.Length - 1; i > insertionIndex; --i)
			{
				onTerms[i] = onTerms[i - 1];
				indices[i] = indices[i - 1];
			}
			onTerms[insertionIndex] = itemToInsert;
			indices[insertionIndex] = onTerms.Length - 1;
		}

		static double Probability_of_Token_Multinomial(int tokID, int classNO, List<int[]> freqOfTokens, List<string> onIDs, int[] freqSumOfClass, bool[] isClassSumAvailable)
		{
			double freqOftok = freqOfTokens[tokID][classNO] + 1;
			double sumOffreqOfAllTokens = 0;
			if (isClassSumAvailable[classNO])
				sumOffreqOfAllTokens = freqSumOfClass[classNO] + onIDs.Length;
			else
			{
				for (int i = 0; i < onIDs.Length; i++)
				{
					sumOffreqOfAllTokens += freqOfTokens[i][classNO] + 1;
					freqSumOfClass[classNO] += freqOfTokens[i][classNO];
				}
				isClassSumAvailable[classNO] = true;
			}
			return (freqOftok / sumOffreqOfAllTokens);
		}

		static void Train_Multinomial()
		{
			Parse();
			OmitStopWords();
			StemDataset();
			StreamReader reader;
			StreamWriter countWriter, probabilitiesWriter, sortedOnTermsWriter, priorsWriter;
			StringBuilder probabilitiesBuilder = new StringBuilder(""), sortedOnTermsBuilder = new StringBuilder("");
			double[] classDocsCount = new double[4]; // Nc 
			int total_category_reviews = 0;  // N
			string review = "";
			string[] tokens;

			foreach (string category in directories)
			{
				countWriter = new StreamWriter(new FileStream(category + @"/Count.review", FileMode.Create));
				total_category_reviews = 0;
				classDocsCount[0] = classDocsCount[1] = classDocsCount[2] = classDocsCount[3] = 0;
				foreach(int ratingInt in indexToClass)
				{
					reader = new StreamReader(new FileStream(category + @"/" + ratingInt + ".review", FileMode.Open));
					while ((review = reader.ReadLine()) != null)
					{
						tokens = review.Split(' ', ',', '"', ':', ';', '.', '\t', '\n', '!', '-', '?', '@', '%', '(', ')', '`', '/', '=', '\\', '~', '#', '$', '%', '^', '&', '*', '_', '|', '[', ']', '{', '}', '<', '>', '+');
						foreach (string tok in tokens)
						{
							int index = GetIndexIfPresent(sorted_on_termStrs, tok);
							if (index == -1)
							{
								int indexToInsert = GetProbableIndex(sorted_on_termStrs, tok);
								InsertIntoVocabulary_Multinomial(sorted_on_termStrs, indices, sorted_on_IDs, frequencies, tok, indexToInsert, classToIndex[ratingInt]);
							}
							else
								++frequencies[indices[index]][classToIndex[ratingInt]];
						}
						++classDocsCount[classToIndex[ratingInt]];
					}
					reader.Close();
					countWriter.WriteLine((int)classDocsCount[classToIndex[ratingInt]]);
					total_category_reviews += (int)classDocsCount[classToIndex[ratingInt]];
				}
				countWriter.WriteLine(total_category_reviews);
				countWriter.Close();

				priorsWriter = new StreamWriter(new FileStream(category + @"/Priors.review", FileMode.Create));
				for (int i = 0; i < 4; ++i)
					priorsWriter.WriteLine(classDocsCount[i] / total_category_reviews);
				priorsWriter.Close();

				probabilitiesWriter = new StreamWriter(new FileStream(category + @"/Probabilities_Multinomial.review", FileMode.Create));
				sortedOnTermsWriter = new StreamWriter(new FileStream(category + @"/SortedOnTerms.review", FileMode.Create));
				double[] probOfTok = new double[4];
				freqSumOfClass[0] = freqSumOfClass[1] = freqSumOfClass[2] = freqSumOfClass[3] = 0;
				isClassSumAvailable[0] = isClassSumAvailable[1] = isClassSumAvailable[2] = isClassSumAvailable[3] = false;
				for (int index = 0; index < sorted_on_IDs.Length; ++index)
				{
					for (int classNO = 0; classNO < 4; ++classNO)
						probOfTok[classNO] = Probability_of_Token_Multinomial(index, classNO, frequencies, sorted_on_IDs, freqSumOfClass, isClassSumAvailable);

					probabilitiesBuilder.Append(index).Append(' ').Append(sorted_on_IDs[index]).Append(' ').Append(probOfTok[0]).Append(' ').Append(probOfTok[1]).Append(' ').Append(probOfTok[2]).Append(' ').Append(probOfTok[3]);
					probabilitiesWriter.WriteLine(probabilitiesBuilder.ToString());
					probabilitiesBuilder.Clear();

					sortedOnTermsBuilder.Append(sorted_on_termStrs[index]).Append(' ').Append(indices[index]);
					sortedOnTermsWriter.WriteLine(sortedOnTermsBuilder.ToString());
					sortedOnTermsBuilder.Clear();

					if (index % 100 == 0)
					{
						probabilitiesWriter.Flush();
						sortedOnTermsWriter.Flush();
					}
				}

				probabilitiesWriter.Close();
				sortedOnTermsWriter.Close();

				frequencies.Clear();
				sorted_on_IDs.Clear();
				sorted_on_termStrs.Clear();
				indices.Clear();
			}
		}

		static void Test_Multinomial()
		{
			Parse_for_Test();
			OmitStopWords_for_test();
			StemDataSet_for_Test();

			int tokenIndex, maxIndex;
			string[] tokens;
			string review, input = "";
			double maxValue;
			double[] scores = new double[4];
			StreamReader reader, probabilitiesReader, sortedOnTermsReader, priorsReader;
			StreamWriter writer;
			StringBuilder builder = new StringBuilder("");

			foreach (string category in directories)
			{
				priorsReader = new StreamReader(new FileStream(category + @"/Priors.review", FileMode.Open));
				for (int i = 0; i < 4; ++i)
					priors[i] = Convert.ToDouble(priorsReader.ReadLine());
				priorsReader.Close();
				probabilitiesReader = new StreamReader(new FileStream(category + @"/Probabilities_Multinomial.review", FileMode.Open));
				sortedOnTermsReader = new StreamReader(new FileStream(category + @"/SortedOnTerms.review", FileMode.Open));
				while ((input = sortedOnTermsReader.ReadLine()) != null)
				{
					tokens = input.Split(' ');
					sorted_on_termStrs.Add(tokens[0]);
					indices.Add(Convert.ToInt32(tokens[1]));
					tokens = probabilitiesReader.ReadLine().Split(' ');
					sorted_on_IDs.Add(tokens[1]);
					probabilities.Add(new double[] { Convert.ToDouble(tokens[2]), Convert.ToDouble(tokens[3]), Convert.ToDouble(tokens[4]), Convert.ToDouble(tokens[5]) });
				}
				sortedOnTermsReader.Close();
				probabilitiesReader.Close();

				foreach(int rating_file in indexToClass)
				{
					reader = new StreamReader(new FileStream(category + @"/" + rating_file + "_Test.review", FileMode.Open));
					while ((review = reader.ReadLine()) != null)
					{
						tokens = review.Split(' ', ',', '"', ':', ';', '.', '\t', '\n', '!', '-', '?', '@', '%', '(', ')', '`', '/', '=', '\\', '~', '#', '$', '%', '^', '&', '*', '_', '|', '[', ']', '{', '}', '<', '>', '+');
						foreach(int ratingInt in indexToClass)
						{
							scores[classToIndex[ratingInt]] = Math.Log(priors[classToIndex[ratingInt]]);
							foreach (string tok in tokens)
								if (!string.IsNullOrWhiteSpace(tok))
								{
									tokenIndex = GetIndexIfPresent(sorted_on_termStrs, tok);
									if (tokenIndex < 0)
										scores[classToIndex[ratingInt]] += Math.Log(1.0 / probabilities.Length);
									else
										scores[classToIndex[ratingInt]] += Math.Log(probabilities[indices[tokenIndex]][classToIndex[ratingInt]]);
								}
						}
						maxValue = scores[maxIndex = 0];
						for (int j = 0; j < 4; ++j)
							if (maxValue < scores[j])
								maxValue = scores[maxIndex = j];
						calculatedLabels.Add(indexToClass[maxIndex]);
						actualLabels.Add(rating_file);
					}
					reader.Close();
				}
				writer = new StreamWriter(new FileStream(category + @"/Labels_Multinomial.review", FileMode.Create));
				double truePositives = 0, falsePositives = 0, trueNegatives = 0, falseNegatives = 0;
				for (int i = 0; i < actualLabels.Length; ++i)
				{
					if (calculatedLabels[i] < 3)
						if (actualLabels[i] < 3)
							++trueNegatives;
						else
							++falseNegatives;
					else
						if (actualLabels[i] < 3)
							++falsePositives;
						else
							++truePositives;
					// int diff = calculatedLabels[i] - actualLabels[i];
					// if(diff == 0)
					// {
					// 	if(actualLabels[i] < 3)
					// 		trueNegatives++;
					// 	else
					// 		truePositives++;
					// }
					// else if (diff < 0)
					// 	falseNegatives++;
					// else
					// 	falsePositives++;
					builder.Append(actualLabels[i]).Append(' ').Append(calculatedLabels[i]);
					writer.WriteLine(builder.ToString());
					builder.Clear();
				}
				writer.Close();
				writer = new StreamWriter(new FileStream(category + @"/Stats_Multinomial.review", FileMode.Create));
				writer.WriteLine("True +ves:   " + truePositives);
				writer.WriteLine("False +ves:  " + falsePositives);
				writer.WriteLine("True -ves:   " + trueNegatives);
				writer.WriteLine("False -ves:  " + falseNegatives);
				writer.WriteLine("%age:        " + (((trueNegatives + truePositives) / (trueNegatives + truePositives + falseNegatives + falsePositives)) * 100));
				writer.WriteLine("Precision:   " + ((truePositives / (truePositives + falsePositives)) * 100));
				writer.WriteLine("Recall:      " + ((truePositives / (truePositives + falseNegatives)) * 100));
				writer.Close();

				actualLabels.Clear();
				calculatedLabels.Clear();
				sorted_on_IDs.Clear();
				sorted_on_termStrs.Clear();
				indices.Clear();
				probabilities.Clear();
			}
		}

		#endregion

		#region Bernoulli API

		public static void InsertIntoVocabulary_Bernoulli(List<string> onTerms, List<int> indices, List<string> onIDs, List<int[]> frequencies, List<bool> found, string itemToInsert, int insertionIndex, int classIndex)
		{
			onIDs.Add(itemToInsert);
			int[] freqs = new int[4];
			for (int i = 0; i < 4; ++i)
				if (i == classIndex)
					freqs[i] = 1;
				else
					freqs[i] = 0;
			frequencies.Add(freqs);
			found.Add(true);
			onTerms.Add(itemToInsert);
			indices.Add(onTerms.Length - 1);
			for (int i = onTerms.Length - 1; i > insertionIndex; --i)
			{
				onTerms[i] = onTerms[i - 1];
				indices[i] = indices[i - 1];
			}
			onTerms[insertionIndex] = itemToInsert;
			indices[insertionIndex] = onTerms.Length - 1;
		}

		static double Probability_of_Token_Bernoulli(int tokID, int classNO, List<int[]> freqOfTokens, List<string> onIDs, int[] freqSumOfClass, bool[] isClassSumAvailable)
		{
			double freqOftok = freqOfTokens[tokID][classNO] + 1;
			double sumOfOccurrencesOfAllTokens = 2;
			if (isClassSumAvailable[classNO])
				sumOfOccurrencesOfAllTokens += freqSumOfClass[classNO];
			else
			{
				for (int i = 0; i < onIDs.Length; ++i)
				{
					freqSumOfClass[classNO] += freqOfTokens[i][classNO];
					sumOfOccurrencesOfAllTokens += freqOfTokens[i][classNO];
				}
				isClassSumAvailable[classNO] = true;
			}
			double probOfTok = freqOftok / sumOfOccurrencesOfAllTokens;
			return probOfTok;
		}

		public static void Train_Bernoulli()
		{
			Parse();
			OmitStopWords();
			StemDataset();
			StreamReader reader;
			StreamWriter countWriter, probabilitiesWriter, sortedOnTermsWriter, priorsWriter;
			StringBuilder probabilitiesBuilder = new StringBuilder(""), sortedOnTermsBuilder = new StringBuilder("");
			double[] classDocsCount = new double[4]; // Nc 
			int total_category_reviews = 0;  // N
			string review = "";
			string[] tokens;

			foreach (string category in directories)
			{
				countWriter = new StreamWriter(new FileStream(category + @"/Count.review", FileMode.Create));
				total_category_reviews = 0;
				classDocsCount[0] = classDocsCount[1] = classDocsCount[2] = classDocsCount[3] = 0;
				foreach(int ratingInt in indexToClass)
				{
					reader = new StreamReader(new FileStream(category + @"/" + ratingInt + ".review", FileMode.Open));
					found.SetValue(false);
					while ((review = reader.ReadLine()) != null)
					{
						tokens = review.Split(' ', ',', '"', ':', ';', '.', '\t', '\n', '!', '-', '?', '@', '%', '(', ')', '`', '/', '=', '\\', '~', '#', '$', '%', '^', '&', '*', '_', '|', '[', ']', '{', '}', '<', '>', '+');
						foreach (string tok in tokens)
						{
							int index = GetIndexIfPresent(sorted_on_termStrs, tok);
							if (index == -1)
							{
								int indexToInsert = GetProbableIndex(sorted_on_termStrs, tok);
								InsertIntoVocabulary_Bernoulli(sorted_on_termStrs, indices, sorted_on_IDs, frequencies, found, tok, indexToInsert, classToIndex[ratingInt]);
							}
							else
								if (!found[indices[index]])
								{
									++frequencies[indices[index]][classToIndex[ratingInt]];
									found[indices[index]] = true;
								}
						}
						++classDocsCount[classToIndex[ratingInt]];
					}
					reader.Close();
					countWriter.WriteLine((int)classDocsCount[classToIndex[ratingInt]]);
					total_category_reviews += (int)classDocsCount[classToIndex[ratingInt]];
				}
				countWriter.WriteLine(total_category_reviews);
				countWriter.Close();

				priorsWriter = new StreamWriter(new FileStream(category + @"/Priors.review", FileMode.Create));
				for (int i = 0; i < 4; ++i)
					priorsWriter.WriteLine(classDocsCount[i] / total_category_reviews);
				priorsWriter.Close();

				probabilitiesWriter = new StreamWriter(new FileStream(category + @"/Probabilities_Bernoulli.review", FileMode.Create));
				sortedOnTermsWriter = new StreamWriter(new FileStream(category + @"/SortedOnTerms.review", FileMode.Create));
				double[] probOfTok = new double[4];
				freqSumOfClass[0] = freqSumOfClass[1] = freqSumOfClass[2] = freqSumOfClass[3] = 0;
				isClassSumAvailable[0] = isClassSumAvailable[1] = isClassSumAvailable[2] = isClassSumAvailable[3] = false;
				for (int index = 0; index < sorted_on_IDs.Length; ++index)
				{
					for (int classNO = 0; classNO < 4; ++classNO)
						probOfTok[classNO] = Probability_of_Token_Bernoulli(index, classNO, frequencies, sorted_on_IDs, freqSumOfClass, isClassSumAvailable);

					probabilitiesBuilder.Append(index).Append(' ').Append(sorted_on_IDs[index]).Append(' ').Append(probOfTok[0]).Append(' ').Append(probOfTok[1]).Append(' ').Append(probOfTok[2]).Append(' ').Append(probOfTok[3]);
					probabilitiesWriter.WriteLine(probabilitiesBuilder.ToString());
					probabilitiesBuilder.Clear();

					sortedOnTermsBuilder.Append(sorted_on_termStrs[index]).Append(' ').Append(indices[index]);
					sortedOnTermsWriter.WriteLine(sortedOnTermsBuilder.ToString());
					sortedOnTermsBuilder.Clear();

					if (index % 100 == 0)
					{
						probabilitiesWriter.Flush();
						sortedOnTermsWriter.Flush();
					}
				}
				probabilitiesWriter.Close();
				sortedOnTermsWriter.Close();

				frequencies.Clear();
				sorted_on_IDs.Clear();
				sorted_on_termStrs.Clear();
				indices.Clear();
				found.Clear();
			}
		}

		public static void Test_Bernoulli()
		{
			Parse_for_Test();
			OmitStopWords_for_test();
			StemDataSet_for_Test();

			int maxIndex;
			string[] tokens;
			string review, input = "";
			double maxValue;
			double[] scores = new double[4];
			StreamReader reader,probabilitiesReader, sortedOnTermsReader, priorsReader;
			StreamWriter writer;
			StringBuilder builder = new StringBuilder("");
			List<string> list;

			foreach (string category in directories)
			{
				priorsReader = new StreamReader(new FileStream(category + @"/Priors.review", FileMode.Open));
				for (int i = 0; i < 4; ++i)
					priors[i] = Convert.ToDouble(priorsReader.ReadLine());
				priorsReader.Close();
				probabilitiesReader = new StreamReader(new FileStream(category + @"/Probabilities_Bernoulli.review", FileMode.Open));
				sortedOnTermsReader = new StreamReader(new FileStream(category + @"/SortedOnTerms.review", FileMode.Open));
				while ((input = sortedOnTermsReader.ReadLine()) != null)
				{
					tokens = input.Split(' ');
					sorted_on_termStrs.Add(tokens[0]);
					indices.Add(Convert.ToInt32(tokens[1]));
					tokens = probabilitiesReader.ReadLine().Split(' ');
					sorted_on_IDs.Add(tokens[1]);
					probabilities.Add(new double[] { Convert.ToDouble(tokens[2]), Convert.ToDouble(tokens[3]), Convert.ToDouble(tokens[4]), Convert.ToDouble(tokens[5]) });
				}
				sortedOnTermsReader.Close();
				probabilitiesReader.Close();

				foreach(int rating_file in indexToClass)
				{
					reader = new StreamReader(new FileStream(category + @"/" + rating_file + "_Test.review", FileMode.Open));
					while ((review = reader.ReadLine()) != null)
					{
						list = GetVocabulary(review.Split(' ', ',', '"', ':', ';', '.', '\t', '\n', '!', '-', '?', '@', '%', '(', ')', '`', '/', '=', '\\', '~', '#', '$', '%', '^', '&', '*', '_', '|', '[', ']', '{', '}', '<', '>', '+'));
						foreach(int ratingInt in indexToClass)
						{
							scores[classToIndex[ratingInt]] = Math.Log(priors[classToIndex[ratingInt]], 2);
							for (int k = 0; k < sorted_on_IDs.Length; ++k)
								if (GetIndexIfPresent(list, sorted_on_IDs[k]) < 0)
									scores[classToIndex[ratingInt]] += Math.Log(1 - probabilities[k][classToIndex[ratingInt]], 2);
								else
									scores[classToIndex[ratingInt]] += Math.Log(probabilities[k][classToIndex[ratingInt]], 2);
						}
						maxValue = scores[maxIndex = 0];
						for (int j = 0; j < 4; ++j)
							if (maxValue < scores[j])
								maxValue = scores[maxIndex = j];
						calculatedLabels.Add(indexToClass[maxIndex]);
						actualLabels.Add(rating_file);
					}
					reader.Close();
				}
				writer = new StreamWriter(new FileStream(category + @"/Labels_Bernoulli.review", FileMode.Create));
				double truePositives = 0, falsePositives = 0, trueNegatives = 0, falseNegatives = 0;
				for (int i = 0; i < actualLabels.Length; ++i)
				{
					if (calculatedLabels[i] < 3)
						if (actualLabels[i] < 3)
							++trueNegatives;
						else
							++falseNegatives;
					else
						if (actualLabels[i] < 3)
							++falsePositives;
						else
							++truePositives;
					builder.Append(actualLabels[i]).Append(' ').Append(calculatedLabels[i]);
					writer.WriteLine(builder.ToString());
					builder.Clear();
				}
				writer.Close();
				writer = new StreamWriter(new FileStream(category + @"/Stats_Bernoulli.review", FileMode.Create));
				writer.WriteLine(truePositives);
				writer.WriteLine(falsePositives);
				writer.WriteLine(trueNegatives);
				writer.WriteLine(falseNegatives);
				writer.WriteLine(((trueNegatives + truePositives) / (trueNegatives + truePositives + falseNegatives + falsePositives)) * 100);
				writer.WriteLine((truePositives / (truePositives + falsePositives)) * 100);
				writer.WriteLine((truePositives / (truePositives + falseNegatives)) * 100);
				writer.Close();

				actualLabels.Clear();
				calculatedLabels.Clear();
				sorted_on_IDs.Clear();
				sorted_on_termStrs.Clear();
				indices.Clear();
				probabilities.Clear();
			}
		}

		#endregion

		static void Pick()
		{
			string input = "", rating = "", ID = "";
			StringBuilder reviewTextBuilder = new StringBuilder("");
			StreamReader reader, pickedReader, unlabeledReader;
			StreamWriter writer;
			int count = 0, index;
			List<bool> foundUnlabeled = new List<bool>();
			List<string> uniqueIDs = new List<string>(), pickedUniqueIDs = new List<string>(), unlabeledUniqueIDs = new List<string>();
			foreach(string category in directories)
			{
				foreach(string training_file in training_files)
				{
					reader = new StreamReader(new FileStream(category + '/' + training_file, FileMode.Open));
					while ((input = reader.ReadLine()) != null)
						if (input.Contains("<unique_id>"))
						{
							ID = reader.ReadLine();
							if (!IsPresentInList(uniqueIDs, ID))
							{
								index = GetProbableIndex(uniqueIDs, ID);
								uniqueIDs.Add(ID);
								for (int i = uniqueIDs.Length - 1; i > index; --i)
									uniqueIDs[i] = uniqueIDs[i - 1];
								uniqueIDs[index] = ID;
								while (!((input = reader.ReadLine()).Contains("</review>"))) ;
							}
						}
					reader.Close();
				}

				for(int k = 0; k < 2; ++k)
				{
					count = 0;
					reader = new StreamReader(new FileStream(category + '/' + picking_files[k], FileMode.Open));
					writer = new StreamWriter(new FileStream(category + picked_files[k], FileMode.Create));
					while ((input = reader.ReadLine()) != null)
						if (input.Contains("<unique_id>"))
						{
							ID = reader.ReadLine();
							while (!((input = reader.ReadLine()).Contains("<rating>"))) ;
							rating = reader.ReadLine();
							while (!((input = reader.ReadLine()).Contains("<review_text>"))) ;
							while (!((input = reader.ReadLine()).Contains("</review_text>")))
								reviewTextBuilder.Append(input).Append(' ');
							if (!IsPresentInList(uniqueIDs, ID))
							{
								if (!IsPresentInList(unlabeledUniqueIDs, ID))
								{
									index = GetProbableIndex(unlabeledUniqueIDs, ID);
									unlabeledUniqueIDs.Add(ID);
									for (int i = unlabeledUniqueIDs.Length - 1; i > index; --i)
										unlabeledUniqueIDs[i] = unlabeledUniqueIDs[i - 1];
									unlabeledUniqueIDs[index] = ID;
									foundUnlabeled.Add(false);
								}
								writer.WriteLine(ID);
								writer.WriteLine(rating);
								writer.WriteLine(reviewTextBuilder.ToString());
								++count;
								if (count % 200 == 0)
									writer.Flush();
							}
							reviewTextBuilder.Clear();
						}
					writer.Close();
					reader.Close();
				}
					
				writer = new StreamWriter(new FileStream(category + @"/TestSet.review", FileMode.Create));
				pickedReader = new StreamReader(new FileStream(category + @"/Picked.review", FileMode.Open));
				while ((input = pickedReader.ReadLine()) != null)
				{
					ID = input;
					rating = pickedReader.ReadLine();
					reviewTextBuilder.Append(pickedReader.ReadLine());
					if (!IsPresentInList(unlabeledUniqueIDs, ID))
					{
						writer.WriteLine(rating);
						writer.WriteLine(reviewTextBuilder.ToString());
						++count;
						if (count % 200 == 0)
							writer.Flush();
					}
					else if(!foundUnlabeled[index = GetIndexIfPresent(unlabeledUniqueIDs, ID)])
					{
						writer.WriteLine(rating);
						writer.WriteLine(reviewTextBuilder.ToString());
						++count;
						if (count % 200 == 0)
							writer.Flush();
						foundUnlabeled[index] = true;
					}
					reviewTextBuilder.Clear();
				}
				pickedReader.Close();
				File.Delete(category + @"/Picked.review");
				unlabeledReader = new StreamReader(new FileStream(category + @"/Picked_unlabeled.review", FileMode.Open));
				while ((input = unlabeledReader.ReadLine()) != null)
				{
					ID = input;
					rating = unlabeledReader.ReadLine();
					reviewTextBuilder.Append(unlabeledReader.ReadLine());
					if (!IsPresentInList(pickedUniqueIDs, ID))
					{
						writer.WriteLine(rating);
						writer.WriteLine(reviewTextBuilder.ToString());
						++count;
						if (count % 200 == 0)
							writer.Flush();
					}
					reviewTextBuilder.Clear();
				}
				unlabeledReader.Close();
				File.Delete(category + @"/Picked_unlabeled.review");
				writer.Close();
				uniqueIDs.Clear();
				pickedUniqueIDs.Clear();
				unlabeledUniqueIDs.Clear();
				foundUnlabeled.Clear();
			}
		}

		static void Main(string[] args)
		{
			Train_Multinomial();
			//Train_Bernoulli();
			//Pick();
			Test_Multinomial();
			//Test_Bernoulli();
		}
	}
}