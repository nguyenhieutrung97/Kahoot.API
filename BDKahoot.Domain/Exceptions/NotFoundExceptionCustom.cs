namespace BDKahoot.Domain.Exceptions
{
    public class NotFoundExceptionCustom(string resourceType, string resourceIdentifier) : Exception($"{resourceType} with id: {resourceIdentifier} doesn't exist.")
    {

    }
}
