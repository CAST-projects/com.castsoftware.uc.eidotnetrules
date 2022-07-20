using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests.TestSources
{
    public class AvoidHavingSameImplementationInAllBranchesOfConditionalStructure_Source
    {

        public void func1() {}
        public void func2() {}
        public void Run() 
        {
            int val = -1;
            if (val == 1)
            {
                func1();
            }
             else if (val == 2)
            {
                func1();
            }
            else if (val == 3) 
            {
                func1();
            }
            else if (val == 4) 
            {
                func1();
            }
            else 
            {
                func1();
            }
        }
    
        public int Run2() 
        {
            int val = -1;
            if (val == 1)
            {
                val = 2;
                return 1;
            }
            else 
            {
                val = 2;
                return 1;
            }
        }
    
        public void Run3() 
        {
            int val = -1;
            int result = val > 12 ? 3 : 3;
        }

        public void Run4()
        {
            int val = -1;
            int result = val > 12 ? 3 : 5;
        }

        public void Run5()
        {
            int val = -1;
            if (val == 1)
            {
                func1();
            }
            else if (val == 2)
            {
                func1();
            }
            else if (val == 3)
            {
                func1();
            }
        }


        public void bar() { }
        public void Run6()
        {
            int i = -1;
            switch (i + 1)
            {
                case 1:
                    bar();
                    break;
                case 2:
                    bar();
                    break;
                case 3:
                    bar();
                    break;
                default:
                    bar();
                    break;
            }
        }

        public void bar2() { }
        public void Run7()
        {
            int i = -1;
            switch (i + 1)
            {
                case 1:
                    bar();
                    break;
                case 2:
                    bar2();
                    break;
                case 3:
                    bar();
                    break;
                default:
                    bar();
                    break;
            }
        }

        public void Run8()
        {
            int i = -1;
            switch (i + 1)
            {
                case 1:
                    bar();
                    break;
                case 2:
                    bar();
                    break;
                case 3:
                    bar();
                    break;                
            }
        }

        public void Run9()
        {
            int i = -1;
            switch (i + 1)
            {
                case 1:
                    bar2();
                    break;
                case 2:
                    bar2();
                    break;
                case 3:
                    bar2();
                    break;
                case 4:
                case 5:
                case 6:
                default:
                    bar2();
                    break;
            }
        }

        public void Run10()
        {
            int val = -1;
            if (val == 1)
            {
                func1();
            }
            else
            {
                func2();
            }

            if (val == 1)
            {
                func1();
            }
            else if (val == 1)
            {
                func1();
            }
            else
            {
                func2();
            }

            if (val == 1)
            {
                func1();
            }
            else if (val == 1)
            {
                func2();
            }
            else
            {
                func1();
            }

            if (val == 1)
            {
                func1();
            }
            else if (val == 1)
            {
                func2();
            }
            else if (val == 1)
            {
                func1();
            }
            else
            {
                func1();
            }
        }

        public void Run11()
        {
            int i = -1;
            switch (i + 1)
            {
                case 1:
                    bar();
                    break;
                case 2:
                    bar();
                    break;
                case 3:
                    bar();
                    break;
                default:
                    bar2();
                    break;
            }

            switch (i + 1)
            {
                case 1:
                    bar();
                    break;
                case 2:
                    bar();
                    break;
                case 3:
                    bar2();
                    break;
                default:
                    bar();
                    break;
            }
        }

    }
 
}
