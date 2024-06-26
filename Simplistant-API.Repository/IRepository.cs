﻿using System.Linq.Expressions;

namespace Simplistant_API.Repository
{
    public interface IRepository<T>
    {
        List<T> GetWhere(Expression<Func<T, bool>> filter);
        void RemoveWhere(Expression<Func<T, bool>> filter);
        bool Upsert(T data);
    }
}
