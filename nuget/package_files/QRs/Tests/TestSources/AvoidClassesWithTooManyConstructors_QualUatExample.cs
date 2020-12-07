using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sonar.Analyzers.CSharp.Common.Tests.UnitTests.Analyzers.AvoidClassesWithTooManyConstructors
{
    class AvoidClassesWithTooManyConstructors_QualUatExample
    {
        public AvoidClassesWithTooManyConstructors_QualUatExample()
        {
            //1er constructeur
        }

        public AvoidClassesWithTooManyConstructors_QualUatExample(string maChaine)
        {
            //2eme constructeur
        }

        public AvoidClassesWithTooManyConstructors_QualUatExample(bool monBooleen)
        {
            //3eme constructeur
        }

        public AvoidClassesWithTooManyConstructors_QualUatExample(int monInt)
        {
            //4eme constructeur
        }

        public AvoidClassesWithTooManyConstructors_QualUatExample(string maChaine, int monInt)
        {
            //5eme constructeur
        }

        public void shit()
        {
            int x = 6;

            int y = 7;
        }
    }
}
