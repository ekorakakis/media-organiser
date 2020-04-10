using System.Threading.Tasks;

namespace MediaOrganiser
{
    public interface IMedium
    {
        Task ProcessAsync();

        bool CanProcess();
    }
}
