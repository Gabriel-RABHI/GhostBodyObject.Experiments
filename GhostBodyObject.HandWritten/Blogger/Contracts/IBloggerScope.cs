using GhostBodyObject.HandWritten.Blogger.Repository;
using System;
using System.Collections.Generic;
using System.Text;

namespace GhostBodyObject.HandWritten.Blogger.Contracts
{
    public interface IBloggerScope : IDisposable
    {
        BloggerTransaction Transaction { get; }

        BloggerRepository Repository { get; }
    }
}
