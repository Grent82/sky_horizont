namespace SkyHorizont.Domain.Intrigue
{
    public interface ISecretsRepository
    {
        Secret Add(Secret secret);
        Secret? GetById(Guid id);
    }
}