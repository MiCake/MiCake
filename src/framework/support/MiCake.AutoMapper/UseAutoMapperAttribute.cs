using AutoMapper;
using MiCake.Core.Modularity;
using System;

namespace MiCake.AutoMapper
{
    /// <summary>
    /// Marking the current assembly needs to be registered by automapper.
    /// <para>
    /// If you use <see cref="Profile"/> to configure the mapping relationship,
    /// you need to mark this attribute in the <see cref="MiCakeModule"/>.
    /// </para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class UseAutoMapperAttribute : Attribute
    {
    }
}
