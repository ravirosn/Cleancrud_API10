using System;
using System.Collections.Generic;
using System.Text;

namespace CleanCrud.Application.Interfaces
{
    public interface IPasswordService
    {
        string HashPassword(string password);
        bool VerifyPassword(string password, string hash);
    }
}
