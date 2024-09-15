using System;

namespace NotLoveBot.Interface
{
    public interface IGetDataProcessing<Data>
    {
        Task<List<Data>> GetList(string data, string botName);
    }
}