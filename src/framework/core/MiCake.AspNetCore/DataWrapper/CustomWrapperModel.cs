using MiCake.Core.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace MiCake.AspNetCore.DataWrapper
{
    /// <summary>
    /// Configure classes for custom wrapper types.
    /// <para>
    /// You can add some custom properties by calling the addproperty method,
    /// Which will be generated automatically when the result is wrapped
    /// </para>
    /// </summary>
    [DebuggerDisplay("{ModelName}")]
    public class CustomWrapperModel
    {
        private readonly Dictionary<string, ConfigWrapperPropertyDelegate> _configProperties;
        private string _modelName;

        /// <summary>
        /// The customer model class name.
        /// <para>
        /// If not specified, it will be generated based on the timestamp.
        /// Example:"MiCakeWrapperResponse_20200202220022"
        /// </para>
        /// </summary>
        public string ModelName
        {
            get
            {
                if (!string.IsNullOrEmpty(_modelName))
                    return _modelName;

                return "MiCakeWrapperResponse_" + DateTime.Now.ToString("yyyyMMddHHmmss");
            }
        }

        public CustomWrapperModel()
        {
            _configProperties = new Dictionary<string, ConfigWrapperPropertyDelegate>();
        }

        public CustomWrapperModel(string modelName) : this()
        {
            CheckValue.NotNullOrEmpty(modelName, nameof(modelName));

            _modelName = modelName;
        }

        public void SetModelName(string modelName)
        {
            CheckValue.NotNullOrEmpty(modelName, nameof(modelName));

            _modelName = modelName;
        }

        /// <summary>
        /// Add the property information to the custom model
        /// </summary>
        /// <param name="propertyName">Property Name.</param>
        /// <param name="valueFrom">Property value source.</param>
        public bool AddProperty(string propertyName, ConfigWrapperPropertyDelegate valueFrom)
        {
            if (_configProperties.ContainsKey(propertyName))
                throw new ArgumentException($"{propertyName} has already in this {nameof(CustomWrapperModel)}");

            return _configProperties.TryAdd(propertyName, valueFrom);
        }

        /// <summary>
        /// Add the properties information to the custom model
        /// </summary>
        public bool AddProperties(Dictionary<string, ConfigWrapperPropertyDelegate> configProperties)
        {
            bool result = true;
            foreach (var configProperty in configProperties)
            {
                result = AddProperty(configProperty.Key, configProperty.Value);

                if (!result) break;
            }

            return result;
        }

        public Dictionary<string, ConfigWrapperPropertyDelegate> GetAllConfigProperties()
            => _configProperties;
    }
}
