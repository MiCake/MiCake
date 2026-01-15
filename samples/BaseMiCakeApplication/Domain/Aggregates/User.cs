using MiCake.Audit;
using MiCake.Audit.SoftDeletion;
using MiCake.Core;
using MiCake.DDD.Domain;
using System;

namespace BaseMiCakeApplication.Domain.Aggregates
{
    /// <summary>
    /// User aggregate root - Manages user information and lifecycle.
    /// </summary>
    /// <remarks>
    /// This aggregate root demonstrates:
    /// - Audit support (IHasCreatedAt, IHasUpdatedAt)
    /// - Soft deletion support (ISoftDeletable)
    /// - Business logic encapsulation
    /// - Factory pattern for creation
    /// </remarks>
    public class User : AggregateRoot<long>, IHasCreatedAt, IHasUpdatedAt, ISoftDeletable
    {
        /// <summary>
        /// Gets the user's name.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets the user's avatar URL or path.
        /// </summary>
        public string Avatar { get; private set; }

        /// <summary>
        /// Gets the user's age.
        /// </summary>
        public int Age { get; private set; }

        /// <summary>
        /// Gets the user's phone number.
        /// </summary>
        public string Phone { get; private set; }

        /// <summary>
        /// Gets the user's password (hashed in production).
        /// </summary>
        public string Password { get; private set; }

        /// <summary>
        /// Gets the account creation time (Audit support).
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets the last modification time (Audit support).
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Gets a value indicating whether the user is soft-deleted (Soft Deletion support).
        /// </summary>
        public bool IsDeleted { get; set; }

        /// <summary>
        /// Gets the email address of the user.
        /// </summary>
        public string Email { get; private set; }

        /// <summary>
        /// Initializes a new instance of the User class.
        /// </summary>
        public User()
        {
        }

        /// <summary>
        /// Internal constructor for creating new users.
        /// </summary>
        private User(string name, string phone, string pwd, string email, int age = 0)
        {
            // Business rule validation
            if (string.IsNullOrEmpty(phone))
                throw new BusinessException("Phone number cannot be empty");

            if (string.IsNullOrEmpty(pwd))
                throw new BusinessException("Password cannot be empty");

            Password = pwd;
            Phone = phone;
            Name = name ?? "Anonymous";
            Email = email;
            Age = age;
        }

        /// <summary>
        /// Sets the user's avatar.
        /// </summary>
        /// <param name="avatar">The avatar URL or path</param>
        public void SetAvatar(string avatar) => Avatar = avatar;

        /// <summary>
        /// Updates the user's basic information.
        /// </summary>
        /// <param name="name">The new name</param>
        /// <param name="age">The new age</param>
        public void ChangeUserInfo(string name, int age)
        {
            if (age < 0 || age > 150)
                throw new BusinessException("Invalid age");

            Name = name;
            Age = age;
        }

        /// <summary>
        /// Changes the user's phone number.
        /// </summary>
        /// <param name="phone">The new phone number</param>
        public void ChangePhone(string phone)
        {
            if (string.IsNullOrEmpty(phone))
                throw new BusinessException("Phone number cannot be empty");

            Phone = phone;
        }

        /// <summary>
        /// Updates the user's email address.
        /// </summary>
        /// <param name="email">The new email address</param>
        public void UpdateEmail(string email)
        {
            if (string.IsNullOrEmpty(email))
                throw new BusinessException("Email cannot be empty");

            Email = email;
        }

        /// <summary>
        /// Factory method to create a new user.
        /// </summary>
        /// <param name="phone">The user's phone number</param>
        /// <param name="pwd">The user's password</param>
        /// <param name="name">The user's name (optional)</param>
        /// <param name="email">The user's email address (optional)</param>
        /// <param name="age">The user's age (optional)</param>
        /// <returns>A new User instance</returns>
        public static User Create(string phone, string pwd, string name = null, string email = null, int age = 0)
        {
            return new User(name, phone, pwd, email, age);
        }
    }
}
