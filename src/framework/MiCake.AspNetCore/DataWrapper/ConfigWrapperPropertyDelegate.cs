namespace MiCake.AspNetCore.DataWrapper
{
    /// <summary>
    /// This delegation is used to configure custom return data property.
    /// You can get some info from <see cref="DataWrapperContext"/>,and return need show info.
    /// <para>
    ///     example: s => "MiCake";
    /// </para>
    /// </summary>
    /// <param name="context"><see cref="DataWrapperContext"/></param>
    /// <returns>Reponse data property Value</returns>
    public delegate object ConfigWrapperPropertyDelegate(DataWrapperContext context);
}
