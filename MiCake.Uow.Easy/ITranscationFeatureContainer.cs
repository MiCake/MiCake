using System;
using System.Collections.Generic;
using System.Text;

namespace MiCake.Uow.Easy
{
    public interface ITranscationFeatureContainer
    {
        void ResigtedTranscationFeature(string key, ITranscationFeature transcationFeature);

        ITranscationFeature GetOrAddTranscationFeature(string key, ITranscationFeature transcationFeature);

        ITranscationFeature GetTranscationFeature(string key);

        void RemoveTranscation(string key);
    }
}
