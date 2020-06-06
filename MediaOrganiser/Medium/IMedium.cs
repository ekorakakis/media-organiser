using System;
using System.Threading.Tasks;

namespace MediaOrganiser
{
    public interface IMedium
    {
        Task ProcessAsync();

        bool CanProcess();

        string Name { get; }

        string FullPath { get; }

        string Extension { get; }

        DateTime DateTaken { get; }

        long Length { get ; }

        bool IsProcessed { get; }
    }
}
