namespace SmartPackageHub.Infrastructures.ActionResults
{
    public class CreatedResultEnvelope
    {
        public Guid Id { get; set; }

        public CreatedResultEnvelope(Guid id)
        {
            Id = id;
        }
    }
}
