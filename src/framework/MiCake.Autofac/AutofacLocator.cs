using System;
using System.Collections.Generic;
using System.Text;
using Autofac;

namespace MiCake.Autofac
{
    public class AutofacLocator : IAutofacLocator
    {
        public static AutofacLocator Instance { get; private set; }

        public ILifetimeScope Locator { get; set; }

        static AutofacLocator()
        {
            Instance = new AutofacLocator();
        }

        public T GetSerivce<T>()
        {
            if (Locator == null)
                throw new ArgumentException("the Locator is null.Please check if you have configured UseAutofac() in the startup.cs file");

            return Locator.Resolve<T>();
        }
    }
}
