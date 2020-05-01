namespace MiCake.AspNetCore.DataWrapper
{
    /// <summary>
    /// This delegation is used to configure custom return data property.
    /// You can get some info from <see cref="DataWrapper"/>,and return need show info.
    /// </summary>
    /// <param name="context"><see cref="DataWrapper"/></param>
    /// <returns>Reponse data property Value</returns>
    public delegate object ConfigWrapperPropertyDelegate(DataWrapperContext context);
}
