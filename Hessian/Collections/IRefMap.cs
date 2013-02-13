namespace Hessian.Collections
{
    public interface IRefMap<T>
    {
        int Add(T entry);
        T Get(int refId);
        int? Lookup(T entry);
    }
}
