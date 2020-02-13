using System.Threading.Tasks;

namespace MiCake.Core.Reactive
{
    public interface IObserverAsync<in T>
    {
        Task OnNext(T value);
    }
}
