﻿using ConnectBot.Domain.Entities;
using ConnectBot.Domain.Interfaces;

namespace ConnectBot.Application.Cache
{
    public enum UserState
    {
        None,
        FeedbackMode,
        ManualCheckInMode
    }

    public class UserCache
    {
        private readonly IUserService _userService;
        private Dictionary<long, UserState> _userModes = new();
        public Dictionary<long, User> _users = new();
        private List<long> _adminChats = new();

        public UserCache(IUserService userService)
        {
            _userService = userService;
        }

        public bool IsUserInMode(long? chatId, UserState userState)
        {
            return chatId.HasValue && _userModes.TryGetValue(chatId.Value, out UserState inState) &&
                   inState == userState;
        }

        public bool IsUserInTypeMode(long? chatId)
        {
            return chatId.HasValue && _userModes.TryGetValue(chatId.Value, out UserState inState) &&
                   inState != UserState.None;
        }

        public void SetUserMode(long? chatId, UserState state)
        {
            if(chatId.HasValue)
                _userModes[chatId.Value] = state;
        }

        public async Task<User?> GetUserByChatId(long chatId, CancellationToken cancellationToken)
        {
            if (!_users.TryGetValue(chatId, out var cachedUser))
            {
                var user = await _userService.GetUserByChatId(chatId, cancellationToken);
                if (user != null)
                {
                    _users[chatId] = user;
                }
                return user;
            }

            return cachedUser;
        }

        public async Task<List<long>> GetAdminChatIds(CancellationToken cancellationToken)
        {
            if (_adminChats.Count == 0)
            {
                _adminChats = (await _userService.GetUserAdmins(cancellationToken)).Select(u => u.ChatId).ToList();
            }

            return _adminChats;
        }

        public async Task<bool> IsAdminChat(long? chatId)
        {
            if (!chatId.HasValue)
                return false;

            if (_adminChats.Count == 0)
            {
                await GetAdminChatIds(CancellationToken.None);
            }

            return _adminChats.Contains(chatId.Value);
        }
    }
}
