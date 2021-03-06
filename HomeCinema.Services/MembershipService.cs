﻿using System;
using System.Linq;
using System.Collections.Generic;
using HomeCinema.Entities;
using HomeCinema.Data.Repositories;
using HomeCinema.Data.Infrastructure;
using HomeCinema.Data.Extensions;
using System.Security.Principal;

namespace HomeCinema.Services
{
    public class MembershipService : IMembershipService
    {
        #region Variables
        private readonly IEntityBaseRepository<User> _userRepository;
        private readonly IEntityBaseRepository<Role> _roleRepository;
        private readonly IEntityBaseRepository<UserRole> _userRoleRepository;
        private readonly IEncyptionService _encryptionService;
        private readonly IUnitOfWork _unitOfWork;
        #endregion
        #region Helper methods
        private void AddUserToRole(User user, int roleId)
        {
            var role = _roleRepository.GetSingle(roleId);
            if (null == role)
            {
                throw new ApplicationException("Role does not exist.");
            }
            var userRole = new UserRole
            {
                RoleId = role.ID,
                UserId = user.ID
            };
            _userRoleRepository.Add(userRole);
        }
        private bool IsPasswordValid(User user, string password)
        {
            return string.Equals(_encryptionService.EncryptPassword(password, user.Salt), user.HashedPassword);
        }
        private bool IsUserValid(User user, string password)
        {
            if (IsPasswordValid(user, password))
            {
                return !user.IsLocked;
            }
            return false;
        }
        #endregion
        public MembershipService(IEntityBaseRepository<User> userRepository, IEntityBaseRepository<Role> roleRepository,
            IEntityBaseRepository<UserRole> userRoleRepository, IEncyptionService encryptionService, IUnitOfWork unitOfWork)
        {
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _userRoleRepository = userRoleRepository;
            _encryptionService = encryptionService;
            _unitOfWork = unitOfWork;
        }
        public User CreateUser(string username, string email, string password, int[] roles)
        {
            var existingUser = _userRepository.GetSingleByUsername(username);
            if (null != existingUser)
            {
                throw new Exception("Username is already in use.");
            }
            var passwordSalt = _encryptionService.CreateSalt();
            var user = new User
            {
                Username = username,
                Salt = passwordSalt,
                Email = email,
                IsLocked = false,
                HashedPassword = _encryptionService.EncryptPassword(password, passwordSalt),
                DateCreated = DateTime.Now
            };
            _userRepository.Add(user);
            _unitOfWork.Commit();

            if (null != roles || roles.Length > 0)
            {
                foreach (var role in roles)
                {
                    AddUserToRole(user, role);
                }
            }
            _unitOfWork.Commit();

            return user;
        }
        public User GetUser(int userId)
        {
            return _userRepository.GetSingle(userId);
        }
        public List<Role> GetUserRoles(string username)
        {
            List<Role> roles = new List<Role>();
            var existingUser = _userRepository.GetSingleByUsername(username);
            if (null != existingUser)
            {
                roles = existingUser.UserRoles.Distinct().ToList();
            }
            return roles;
        }

        public MembershipContext ValidateUser(string username, string password)
        {
            var membershipContext = new MembershipContext();
            var user = _userRepository.GetSingleByUsername(username);
            if (null != user && IsUserValid(user, password))
            {
                var userRoles = GetUserRoles(user.Username);
                membershipContext.User = user;

                var identity = new GenericIdentity(user.Username);
                membershipContext.Principal = new GenericPrincipal(
                    identity,
                    userRoles.Select(x => x.Name).ToArray());
            }
            return membershipContext;
        }
    }
}
