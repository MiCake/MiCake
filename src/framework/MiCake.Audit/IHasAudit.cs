﻿namespace MiCake.Audit
{
    /// <summary>
    /// Mark a class with creation time and modify time.
    /// </summary>
    public interface IHasAudit : IHasCreationTime, IHasModificationTime
    {
    }
}
