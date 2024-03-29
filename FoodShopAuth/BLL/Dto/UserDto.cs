﻿using DAL.Enum;

namespace BLL.Dto
{
    public class UserDto
    {
        public string Login { get; set; }
        public string Password { get; set; }
        public string Name { get; set; }
        public string Surname { get; set; }
        public Role Role { get; set; }
    }
}