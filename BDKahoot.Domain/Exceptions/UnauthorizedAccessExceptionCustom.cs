using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BDKahoot.Domain.Exceptions
{

    public class UnauthorizedAccessExceptionCustom(string message) : Exception(message)
    {

    }
}
