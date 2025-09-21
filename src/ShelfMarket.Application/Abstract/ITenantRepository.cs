using ShelfMarket.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShelfMarket.Application.Interfaces;

public interface ITenantRepository : IRepository<ShelfTenant>
{
}
