using MiCake.Core.Abstractions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace MiCake.Core.Abstractions
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
    }
}
