﻿using MiCake.Core.DependencyInjection;
using System.Reflection;

namespace MiCake.Core
{
    /// <summary>
    /// The configuration of building the core program of micake
    /// </summary>
    public class MiCakeApplicationOptions
    {
        /// <summary>
        /// Configuration items for auto injection service
        /// 
        /// When a class that implements an <see cref="ITransientService"/> or <see cref="ISingletonService"/>
        /// or <see cref="IScopedService"/> interface will be injected automatically.
        /// But we need to determine which type of service this class is.
        /// 
        /// defalut: find class all interfaces.The service whose interface name contains the class name.
        /// </summary>
        public FindAutoServiceTypesDelegate FindAutoServiceTypes { get; set; }

        /// <summary>
        /// Assemblies of domain layer
        ///Providing this parameter will facilitate micake to better scan related domain objects in the program.
        /// </summary>
        public Assembly[] DomianLayerAssemblies { get; set; }
    }
}