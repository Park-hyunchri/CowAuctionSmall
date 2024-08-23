using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CowAuctionSmall.Models.Structures;

namespace CowAuctionSmall.Models.XMLParser
{
    public class XmlParserCont
    {

       
        private BoardXmlParser _board;
        private UserXmlParser _userInfo;
        private NLogger logger;

        public XmlParserCont(BoardXmlParser boardXml,UserXmlParser userXml) 
        {
            _board = boardXml;
            _userInfo = userXml;
            logger = NLogger.Instance;
        }


        public (BoardList board, UserInfo userInfo) XmlPaserResult()
        {
            BoardList? boardResult;
            UserInfo? userInfoResult;

            string? baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string? boardXmlPath = Path.Combine(baseDir, "Config", "Board.XML");
            string? userXmlPath = Path.Combine(baseDir, "Config", "users.XML");
            try
            {
                boardResult = _board.ParseXml(boardXmlPath);
                userInfoResult = _userInfo.ParseXml(userXmlPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                boardResult = null; // null 값 할당
                userInfoResult = null;
                logger.LogError("XmlPaserResult : " + ex.ToString());
            }

            return (boardResult, userInfoResult);
        }
    }
}
