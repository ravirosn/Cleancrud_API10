using CleanCrud.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace CleanCrud.Application.Interfaces
{
    public interface IJwtService
    {
        //string GenerateToken(string userName);
        string GenerateToken(User user);
    }
}
