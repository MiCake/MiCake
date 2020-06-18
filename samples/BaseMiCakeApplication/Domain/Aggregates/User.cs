using MiCake.AspNetCore.Identity;
using MiCake.Audit;
using MiCake.Identity.Authentication;
using System;

namespace BaseMiCakeApplication.Domain.Aggregates
{
    public class User : MiCakeUser<Guid>, IHasCreationTime, IHasModificationTime
    {
        [JwtClaim]
        public string Name { get; private set; }

        public string Avatar { get; private set; }

        public int Age { get; private set; }

        public string Phone { get; private set; }

        public string Password { get; private set; }

        public DateTime CreationTime { get; set; }

        public DateTime? ModificationTime { get; set; }

        public User()
        {
        }

        internal User(string name, string phone, string pwd, int age)
        {
            //some check rule...

            Id = Guid.NewGuid();
            Password = pwd;
            Phone = phone;
            Name = name;
            Age = age;
        }

        public void SetAvatar(string avatar) => Avatar = avatar;

        public void ChangeUserInfo(string name, int age)
        {
            Name = name;
            Age = age;
        }

        public void ChangePhone(string phone)
        {
            //some check rule...

            Phone = phone;
        }

        public static User Create(string phone,
                                  string pwd,
                                  string name = null,
                                  int age = 0)
        {
            return new User(name, phone, pwd, age);
        }
    }
}
