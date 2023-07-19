using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeSamples
{
    public class Looping
    {
        public void ForLoop()
        {
            Console.WriteLine("ForLoop:");
            for (int i = 1; i < 10; i++)
            {
                Console.WriteLine(i);
            }
        }

        public void NestedLoop()
        {
            Console.WriteLine("NestedLoop:");
            // Outer loop
            for (int i = 1; i <= 5; ++i)
            {
                Console.WriteLine("Outer: " + i);  // Executes 5 times

                // Inner loop
                for (int j = 1; j <= 6; j++)
                {
                    Console.WriteLine(" Inner: " + j); // Executes 30 times (5 * 6)
                }
            }
        }

        public void ForEachLoop()
        {
            Console.WriteLine("ForEachLoop:");
            string[] people = { "Adam", "Julie", "Bill", "Tom", "Lois", "Frank", "Mark", "Samantha" };
            foreach (string person in people)
            {
                Console.WriteLine(person);
            }
        }

        // jump out of loop when i == 5
        public void BreakLoop()
        {
            Console.WriteLine("BreakLoop:");
            for (int i = 1; i <= 10; i++)
            {
                if (i == 5)
                {
                    break;
                }
                Console.WriteLine(i);
            }
        }

        // skip value of 5
        public void ContinueLoop()
        {
            Console.WriteLine("ContinueLoop:");
            for (int i = 1; i <= 10; i++)
            {
                if (i == 5)
                {
                    continue;
                }
                Console.WriteLine(i);
            }
        }

        public void BreakInWhileLoop()
        {
            Console.WriteLine("BreakInWhileLoop:");
            int i = 0;
            while (i <= 10)
            {
                Console.WriteLine(i);
                i++;
                if (i == 5)
                {
                    break;
                }
            }
        }

        public void ContinueWhileLoop()
        {
            Console.WriteLine("ContinueWhileLoop:");
            int i = 0;
            while (i <= 10)
            {
                if (i == 5)
                {
                    i++;
                    continue;
                }
                Console.WriteLine(i);
                i++;
            }
        }

    }
}
