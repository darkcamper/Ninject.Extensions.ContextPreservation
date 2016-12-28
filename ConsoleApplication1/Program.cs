using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
//using Ninject.Extensions.ContextPreservation;

namespace ConsoleApplication1
{
    class Program
    {
        static void Main(string[] args)
        {
        //    var asm = typeof(ContextPreservingResolutionRoot).Assembly;
          
            var asm = Assembly.Load("Ninject.Extensions.ContextPreservation");
            var ll = asm.ExportedTypes.Where(x => !x.IsAbstract).ToArray();

        }
    }
}
