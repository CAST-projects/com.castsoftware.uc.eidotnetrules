using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests.UnitTest.Sources
{
    public class AvoidNullPointerDereference_Source
    {
        object foo2 = null;
        object foo3;
        static object foo4 = null;
        void f()
        {
            object foo = null;
            int i = 0;
            if (i > 0)
            {
                foo.ToString(); // VIOLATION IT IS NULL
            }
            else
            {
                foo = new object();
            }
        }

        void f2()
        {
            int i = 0;

            if (i > 0)
            {
                foo2.ToString(); //NO VIOLATION cause we don't know
            }
            else
            {
                foo2 = new object();
            }
        }

        void f3()
        {
            int i = 0;
            foo3 = null;
            if (i > 0)
            {
                foo3.ToString(); // VIOLATION IT IS NULL
            }
            else
            {
                foo3 = new object();
            }
        }

        void f4()
        {
            object foo = null;
            int i = 0;
            int j = 0;
            if (i > 0)
            {
                foo = new object();
            }

            if (j == 0)
            {
                foo.ToString(); // VIOLATION 
            }

        }

        void f5()
        {
            object foo = null;
            int i = 0;
            {
                foo = new object();
            }
            if (i == 0)
            {
                foo.ToString(); //NO VIOLATION 
            }
        }

        void f6()
        {
            object foo = null;
            int i = 0;
            if (i == 0)
            {
                foo = new object();
                foo.ToString(); //NO VIOLATION 
            }
        }

        void f7()
        {
            object foo = null;
            int i = 0;
            if (i > 0)
            {
                foo.ToString(); //VIOLATION 
                foo = new object();
            }

        }

        void f8()
        {
            object foo = new object();
            int i = 0;
            if (i == 0)
            {
                foo.ToString(); //NO VIOLATION 
            }

        }

        void f9()
        {
            object foo = null;
            if (foo.ToString() == "") //VIOLATION 
            {
                foo = new object();
            }
        }

        void f10()
        {
            object foo = null;
            if (foo == null)
                foo = new object();

            foo.ToString(); //NO VIOLATION 
        }

        void f11()
        {
            object foo = null;
            if (foo == null)
                foo = new object();
            int i = 0;
            if (i >= 0)
                foo.ToString(); //NO VIOLATION 
        }

        void f12()
        {
            object foo = null;
            int i = 0;
            while (i > 0)
            {
                foo = new object();
            }
            foo.ToString(); // VIOLATION 
        }

        void f13()
        {
            object foo = null;
            int i = 0;
            do
            {
                foo = new object();
            } while (i > 0);
            foo.ToString(); //NO VIOLATION 
        }

        void f14()
        {
            int caseSwitch = 1;
            object foo = null;
            switch (caseSwitch)
            {
                case 1:
                    foo.ToString(); // VIOLATION 
                    break;
                case 2:
                case 3:
                    foo = new object();
                    foo.ToString(); //NO VIOLATION 
                    break;
                default:
                    foo = new object();
                    break;
            }
            foo.ToString(); // VIOLATION 
        }

        void f15()
        {
            object foo = null;
            int i = 0;
            for (int j = 0; j < i; j++)
            {
                foo = new object();
            }
            foo.ToString(); // VIOLATION 
        }

        void f16()
        {
            object foo = null;
            List<int> list = new List<int>();
            foreach (var j in list)
            {
                foo = new object();
            }
            foo.ToString(); // VIOLATION 
        }

        void f17()
        {
            object foo = null;
            List<int> list = null;
            foreach (var j in list)
            {
                foo = new object();
            }

            if (list[0] == 0)
            {
                foo = new object();
            }
        }

        static void func()
        {
            int i = 0;
        }

        void f18()
        {
            foo4.ToString(); //NO VIOLATION cause we don't know 
            AvoidNullPointerDereference_Source.func();
        }

        string func2(out string msg)
        {
            msg = String.Empty;
            return msg;
        }

        void f19()
        {
            string msg = null;
            func2(out msg);
            msg.ToString(); // NO VIOLATION
        }

        void f20()
        {
            foo2 = null;
            if (foo2 != null)
            {
                foo2.ToString(); //NO VIOLATION
            }
            else if (foo2 == null)
            {
                foo2.ToString(); //VIOLATION
            }
        }

        void f21()
        {
            object foo = null;
            int i = 0;
            if (i > 0)
            {
                foo = new object();
            }
            else
            {
                foo.ToString(); // VIOLATION
            }

            if (foo == null)
            {
                foo = new object();
            }
            else
            {
                foo.ToString(); //NO VIOLATION
            }

        }

        void f22()
        {
            object foo = null;
            if (foo == null)
            {
                int i = 0;
            }
            foo.ToString(); //VIOLATION
        }

        void f23()
        {
            int[] tab = null;
            int i = 0;
            if (i > 0)
            {
                tab = new int[5];
            }

            if (tab[0] == 0) // VIOLATION
            {
                tab[1] = -1; // VIOLATION
                tab[2] = 10; // VIOLATION
            }
            else
            {
                tab[0] = 0; // VIOLATION
            }
        }

        int func3(int val)
        {
            val = val * 2;
            return val;
        }
        // test for unresolved symbol Klass
        void f24(Klass klass)
        {
            if (klass.dict != null)
            {
                if (klass.dict.ContainsKey("toto"))
                {
                    func3(klass.dict["toto"]);
                }
            }
        }

        void f25()
        {
            object foo = null;
            int i = 0;
            List<int> list = new List<int>() { 1, 2, 3, 4 };
            foreach (var j in list)
            {
                if (i == 0)
                {
                    while (i > 0)
                    {
                        foo = null;
                        foo = new object();
                        if (j > 2)
                        {
                            foreach (var k in list)
                            {
                                foo = new object();
                                foo.ToString(); //NO VIOLATION 
                            }
                        }
                    }
                }
            }
        }

        void f26()
        {
            foo3 = null;
            if (!string.IsNullOrEmpty(foo3?.ToString()))
            {
                foo3.ToString();
            }
        }

        public class StateObject<T>
        {
            public T Value { get; set; }
            public T att;
        }
        public StateObject<string> strvar { get; set; }
        public StateObject<string> toto { get; set; }
        void func4(string msg) { }
        void f27()
        {
            toto.Value = null;
            func4(strvar.Value);
            strvar.Value.ToString();

            toto.att = null;
            strvar.att.ToString();
        }

        void f28()
        {
            object foo = null;
            if (foo == null)
            {
                return;
            }

            foo.ToString(); //NO VIOLATION 
        }

        void f29()
        {
            this.foo3 = null;
            if (this.foo3 == null)
            {
                foo3 = new object();
            }

            this.foo3.ToString(); //NO VIOLATION 
        }

        void f30()
        {
            object foo = null;
            if (foo != null)
            {
                return;
            }

            foo.ToString(); //VIOLATION 
        }

        void f31()
        {
            toto.Value = null;
            toto.att = null;
            if (toto.Value == null || toto.att == null || toto.att.ToString() == "" || toto.Value.ToString() == "") //NO VIOLATION
            {
                toto.Value = "toto";
                toto.att = "toto";
            }

            if (toto.att != null && toto.att.ToString() != "toto") //NO VIOLATION
            {
                toto.att = "toto";
            }

            if (toto.Value == null || toto.Value.ToString() == "" || toto.att != null && toto.att.ToString() != "toto") //NO VIOLATION
            {
                toto.att = "toto";
            }
        }

        void f32()
        {
            object foo = null;
            object foo4 = null;
            object foo5 = new object();
            int i = 0;
            if (i == 0)
            {
                foo = foo5;
                foo.ToString(); //NO VIOLATION 
                foo = foo4;
                foo.ToString(); //VIOLATION     
            }
        }

        public StateObject<string> createStateObj()
        {
            return new StateObject<string>();
        }

        void f33()
        {
            this.toto = null;
            if (this.toto == null || this.toto.Value == null)
            {
                this.toto = createStateObj();
            }

            if (this.toto == null || this.toto.Value == null)
            {
                foo3 = new object();
            }

            if (foo3 != null)
            {
                string name = toto.Value.ToString(); //NO VIOLATION 
            }

        }

        public string formatedAccount;
        public string maskedFormatedAccount;
        public bool SetAccount(string country, string account)
        {

            string bankCode = null;
            string branchCode = null;
            string accountNumber = null;
            string checkDigit = null;
            string format;

            if (account == null || account.Length == 0)
            {
                this.formatedAccount = string.Empty;
                this.maskedFormatedAccount = string.Empty;
                return false;
            }

            this.formatedAccount = account;

            switch (country)
            {
                case "DE":

                    format = "{0} {1}";

                    if (account.Length >= 18)
                    {
                        bankCode = account.Substring(0, 8);
                        accountNumber = account.Substring(8, 10);

                        formatedAccount = string.Format(format, bankCode, accountNumber);

                        maskedFormatedAccount = string.Format(format,
                            "".PadLeft(bankCode.Length, 'X'),
                            accountNumber.Substring(accountNumber.Length - 3).PadLeft(accountNumber.Length, 'X'));
                    }
                    break;

                case "ES":

                    format = "{0} {1} {2} {3}";

                    if (account.Length >= 8)
                    {
                        bankCode = account.Substring(0, 4);
                        branchCode = account.Substring(4, 4);
                    }

                    if (account.Length == 20)
                    {
                        checkDigit = account.Substring(8, 2);
                        accountNumber = account.Substring(10, 10);

                        formatedAccount = string.Format(format, bankCode, branchCode, checkDigit, accountNumber);

                        maskedFormatedAccount = string.Format(format,
                            "".PadLeft(bankCode.Length, 'X'),
                            "".PadLeft(branchCode.Length, 'X'),
                            "".PadLeft(checkDigit.Length, 'X'),
                            accountNumber.Substring(accountNumber.Length - 3).PadLeft(accountNumber.Length, 'X'));
                    }
                    break;

                case "IT":

                    format = "{0} {1} {2} {3}";

                    if (account.Length == 23)
                    {
                        bankCode = account.Substring(1, 5);
                        branchCode = account.Substring(6, 5);
                        checkDigit = account.Substring(0, 1);
                        accountNumber = account.Substring(11, 12);

                        formatedAccount = string.Format(format,
                            account.Substring(0, 1),
                            account.Substring(1, 5),
                            account.Substring(6, 5),
                            account.Substring(11, 12));
                    }
                    break;

                case "BE":

                    format = "{0}-{1}-{2}";

                    if (account.Length >= 3)
                    {
                        bankCode = account.Substring(0, 3);
                    }

                    if (account.Length == 12)
                    {
                        accountNumber = account.Substring(3, 7);
                        checkDigit = account.Substring(10, 2);

                        formatedAccount = string.Format(format, bankCode, accountNumber, checkDigit);

                        maskedFormatedAccount = string.Format(format,
                            "".PadLeft(bankCode.Length, 'X'),
                            accountNumber.Substring(accountNumber.Length - 3).PadLeft(accountNumber.Length, 'X'),
                            "".PadLeft(checkDigit.Length, 'X'));
                    }
                    break;

                case "PT":

                    if (account.Length == 21)
                    {
                        bankCode = account.Substring(0, 4);
                        branchCode = account.Substring(4, 4);
                        accountNumber = account.Substring(8, 11);
                        checkDigit = account.Substring(19, 2);

                        formatedAccount = bankCode + "." + branchCode + "." + accountNumber + "." + checkDigit;

                        maskedFormatedAccount = string.Format("{0}.{1}.{2}.{3}",
                           "".PadLeft(bankCode.Length, 'X'),
                           "".PadLeft(branchCode.Length, 'X'),
                           accountNumber.Substring(accountNumber.Length - 3).PadLeft(accountNumber.Length, 'X'),
                           "".PadLeft(checkDigit.Length, 'X'));
                    }
                    break;

                default:
                    // Format FR par défaut
                    format = "{0} {1} {2}";

                    if (account.Length >= 21)
                    {
                        bankCode = account.Substring(0, 5);
                        branchCode = account.Substring(5, 5);
                        accountNumber = account.Substring(10, 11);

                        formatedAccount = string.Format(format, bankCode, branchCode, accountNumber);

                        maskedFormatedAccount = string.Format(format,
                           "".PadLeft(bankCode.Length, 'X'),
                           "".PadLeft(branchCode.Length, 'X'),
                           accountNumber.Substring(accountNumber.Length - 3).PadLeft(accountNumber.Length, 'X'));
                    }

                    if (account.Length == 23)
                    {
                        checkDigit = account.Substring(21, 2);
                        formatedAccount += " " + checkDigit;
                        maskedFormatedAccount += " XX";
                    }

                    break;
            }

            string BankCode = bankCode ?? string.Empty;
            string BranchCode = branchCode ?? string.Empty;
            string AccountNumber = accountNumber ?? string.Empty;
            string CheckDigit = checkDigit ?? string.Empty;

            if (AccountNumber.Length > 3)
            {
                string maskedAccountNumber = AccountNumber.Substring(AccountNumber.Length - 3).PadLeft(AccountNumber.Length, 'X');
            }

            return true;
        }
        public bool funcBool() { return true; }
        void f34()
        {
            if (this.toto != null)
            {
                this.toto = createStateObj();
            }
            else
            {
                this.toto.Value = "toto";//Violation
            }

            if (toto != null)
            {
                toto = createStateObj();
            }
            else
            {
                toto.Value = "toto";//Violation
            }

            if (this.toto != null)
            {
                this.toto = createStateObj();
            }
            else if (this.toto == null)
            {
                this.toto.Value = "toto";//Violation
            }

            if (toto != null)
            {
                toto = createStateObj();
            }
            else if (toto != null)
            {
                toto.Value = "toto";//No Violation
            }

            if (this.toto != null && strvar != null)
            {
                this.toto = createStateObj();
            }
            else
            {
                this.toto.Value = "toto";//Violation
            }

            if (strvar != null && this.toto != null)
            {
                this.toto = createStateObj();
            }
            else
            {
                this.toto.Value = "toto";//Violation
            }

            if (toto != null || strvar != null)
            {
                toto = createStateObj();
            }
            else
            {
                toto.Value = "toto";//Violation
            }
        }

        void f35()
        {
            bool ind = true;
            StateObject<string> obj1 = createStateObj();
            StateObject<string> obj2 = createStateObj();
            StateObject<string> obj3 = createStateObj();
            this.toto.ToString();
            try
            {
                func();
                if (obj1 != null && this.strvar.att.Length != 0 || ind)
                {
                    if(strvar.att.ToLower() == "att")
                    {
                        func();
                        if(this.toto!=null && this.toto.Value!=null)
                        {
                            if(!((string)this.toto.Value).IsNormalized() && obj2.ToString().Contains("lv"))
                            {
                                if (ind && ((string)this.toto.Value).IsNormalized())
                                {
                                    func();
                                    if (funcBool()) func();
                                }
                                else
                                {
                                    func();
                                    if(funcBool())
                                    {
                                        func();
                                    }

                                    if (ind && obj3.att != null && obj3.att.Length != 0)
                                    {
                                        try
                                        {
                                            if(obj3.att != null)
                                            {
                                                func();
                                                if (ind)
                                                    func();
                                            }
                                            func();
                                        }
                                        catch(Exception err)
                                        {
                                            func();
                                        }
                                    }
                                }

                            }
                            else
                            {
                                this.toto.att = "att";
                                func();
                            }
                        }
                        else
                        {
                            func();
                            this.toto.Value = "titi"; // violation
                        }
                    }
                    else
                    {
                        func();
                    }
                }
                else
                {
                    if (this.toto != null && this.toto.Value != null)
                    {
                        
                        if (!((string)this.toto.Value).IsNormalized())
                        {
                            if (ind && ((string)this.toto.Value).Length == 0)
                            {
                                if (funcBool()) func();
                            }
                            else
                            {
                                if (funcBool())
                                {
                                    func();
                                }

                                if (ind && strvar.att != null && strvar.att.Length != 0)
                                {
                                    try
                                    {
                                        if (strvar.att != null)
                                        {
                                            if (strvar.att.Length!=0)
                                                func();
                                        }
                                    }
                                    catch (Exception err)
                                    {
                                        func();
                                    }
                                }
                            }
                        }
                        else
                        {
                            this.toto.att = "att";
                            try
                            {
                                if (strvar != null) strvar.Value = "value";
                                else func();
                            }
                            catch { func(); }
                            func();
                        }
                        
                    }
                    else
                    {
                        this.toto.Value = "toto";//Violation
                    }
                }
            }
            catch (Exception err)
            {
                func();
            }

        }
	
	void f36(Data A)
        {    
            object foo = null;
            var isNotNullFoo = foo != null || foo.ToString(); // VIOLATION
            if (isNotNullFoo)
            {
                foo.ToString(); // NOT VIOLATION
            }
            else
            {
                foo = new object();
            }
        }
	
	void f37(Data A)
        {    
            object foo = null;
            var isNotNullFoo = foo != null;
            if (isNotNullFoo)
            {
                foo.ToString(); // NOT VIOLATION
            }
            else
            {
                foo.ToString(); //VIOLATION
            }
        }
	
        private IData f38(IData processedData, string keyName)
        {
            object foo = null;
            var isNotNullFoo = foo != null && foo.ToString();
            if (foo != null)
            {
                foo.ToString(); // NOT VIOLATION
            }
            else if(isNotNullFoo)
            {
                foo.ToString(); // NOT VIOLATION
            }
	    else
            {
                foo.ToString(); // VIOLATION
            }
        }

        void f39(Data A)
        {
            object foo = null;
            var isNotNullFoo = foo != null;
            if (isNotNullFoo)
            {
                string msg;
                if (string.IsNullOrWhiteSpace(foo.ToString())) // NO VIOLATION
                {
                    msg = foo.ToString(); // NO VIOLATION
                }
            }

            bool isNotNullFoo2;
            isNotNullFoo2 = foo != null;
            if (isNotNullFoo2)
            {
                foo.ToString(); // NO VIOLATION
                string msg = "";
                if (msg.Length == 0)
                {
                    msg = foo.ToString(); // NO VIOLATION
                }
            }
            else
            {
                foo.ToString(); // VIOLATION
            }


        }
    }
}
