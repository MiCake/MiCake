namespace MiCake.Audit.SoftDeletion
{
    /// <summary>
    /// Define a class is soft deletable.
    /// <para>
    /// It indicates whether the entity is deleted without physically removing it from the database.
    /// </para>
    /// </summary>
    public interface ISoftDeletable
    {
        /// <summary>
        /// Indicates whether the entity is deleted.
        /// </summary>
        public bool IsDeleted { get; set; }
    }
}
