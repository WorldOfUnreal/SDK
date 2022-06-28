using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using System.CodeDom.Compiler;
using System.Reflection;

namespace CandidSDK
{
    internal class DynamicClassGenerator
    {
        
        public static object Resolver(string strClase, string NameClass)
        {
            //Parámetros del compilador
            CompilerParameters objParametros = new CompilerParameters()
            {
                GenerateInMemory = true,
                GenerateExecutable = false,
                IncludeDebugInformation = false
        
            };
            //Compilo todo y ejecuto el método
            CodeDomProvider objCompiler = CodeDomProvider.CreateProvider("CSharp");
            CompilerResults objResultados = objCompiler.CompileAssemblyFromSource(objParametros, strClase);
            object objClase = objResultados.CompiledAssembly.CreateInstance(NameClass,false, BindingFlags.CreateInstance, null, null, null, null);
            return objClase.GetType().InvokeMember("Ejecutar", BindingFlags.InvokeMethod, null, objClase, null);
            }

        //public static object ResolverCandid { 
        
        //}
    }
}
