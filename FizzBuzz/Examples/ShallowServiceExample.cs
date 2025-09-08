using System;
using System.Collections.Generic;

namespace ShallowServiceExample
{
    public class UserService
    {
        private readonly IUserRepository _repo;
        public UserService(IUserRepository repo) { _repo = repo; }
        public User GetById(int id) => _repo.GetById(id);
        public void Add(User user) => _repo.Add(user);
        public void Remove(int id) => _repo.Remove(id);
        public IEnumerable<User> GetAll() => _repo.GetAll();
    }

    // Entity

    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    // Repository interface

    public interface IUserRepository
    {
        User GetById(int id);
        void Add(User user);
        void Remove(int id);
        IEnumerable<User> GetAll();
    }

    // Concrete repository

    public class UserRepository : IUserRepository
    {
        private readonly Dictionary<int, User> _users = new();
        public User GetById(int id) => _users.TryGetValue(id, out var user) ? user : null;
        public void Add(User user) => _users[user.Id] = user;
        public void Remove(int id) => _users.Remove(id);
        public IEnumerable<User> GetAll() => _users.Values;
    }
}

