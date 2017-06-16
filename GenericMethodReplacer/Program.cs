using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using DelegateDecompiler;
using Mono;
using Mono.Reflection;

namespace GenericMethodReplacer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            //Define a mock class(MockRunner) to substitue the original class(Runner)
            //var result = (new Runner()).Run();
            //Following method only replace the method call of MainType.RunGen<String> to call of SubType.RunGen<String>
            var result = (new MockRunner()).Run();
        }
    }

    public class ReplaceMainTypesVisitor : ExpressionVisitor
    {
        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            var mi = node.Method;
            if (mi.DeclaringType == typeof(MainType)
                && mi.Name == "RunGen")
            {
                //ParameterExpression arg = Expression.Parameter(typeof (String), "t");
                ConstantExpression arg = Expression.Constant("ttt");

                MethodInfo genMethod = typeof (SubType).GetMethod("RunGen").MakeGenericMethod(typeof (String));

                MethodCallExpression callNode = Expression.Call(genMethod,arg);

                return callNode;
            }
            return base.VisitMethodCall(node);
        }
    }

    public interface IRunner
    {
        string Run();
    }

    public class Runner:IRunner
    {
        public string Run()
        {
            var s1 = MainType.RunGen<String>("t");
            var s2 = MainType.Run();
            var s3 = MainType.RunGen<String>("ta");
            return String.Format("{0} - {1} - {2}", s1, s2, s3);
        }
    }

    public class MockRunner:IRunner
    {
        public string Run()
        {
            var runnermi = typeof(Runner).GetMethod("Run");

            LambdaExpression runnerexpression = MethodBodyDecompiler.Decompile(runnermi);
            Expression body = runnerexpression.Body;
            ReplaceMainTypesVisitor visitor = new ReplaceMainTypesVisitor();
            Expression visitedBody = visitor.Visit(body);
            Expression<Func<String>> newLambda = Expression.Lambda<Func<String>>(visitedBody);
            var func = newLambda.Compile() as Func<String>;

            return func();
        }
    }

    public class MainType
    {
        public static string Run()
        {
            return "main";
        }

        public static string RunGen<T>(T t)
        {
            return "main generic";
        }
    }

    public class SubType
    {
        public static string Run()
        {
            return "sub";
        }

        public static string RunGen<T>(T t)
        {
            return "sub generic";
        }
    }
}
