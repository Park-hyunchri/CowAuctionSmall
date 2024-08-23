using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using CowAuctionSmall.Models.Structures;

namespace CowAuctionSmall.Models.XMLParser
{
    public class BoardXmlParser
    {
        public  BoardList ParseXml(string xmlFilePath)
        {
            BoardList boardList = new BoardList();
            boardList.MultiBoards = new List<Board>();

            XDocument xmlDoc = XDocument.Load(xmlFilePath);

            var sizeElement = xmlDoc.Root.Element("Size");
            if (sizeElement != null)
            {
                boardList.Size = sizeElement.Value;
            }

            var multiBoards = xmlDoc.Root.Elements("MultiBoard");
            foreach (var multiBoard in multiBoards)
            {
                Board board = new Board();
                board.Name = multiBoard.Attribute("Name")?.Value;
                board.Rows = new List<int[]>();

                var rowIdxs = multiBoard.Elements("RowIdx");
                foreach (var rowIdx in rowIdxs)
                {
                    var rowDataStr = rowIdx.Value;
                    if (!string.IsNullOrEmpty(rowDataStr))
                    {
                        var rowData = rowDataStr.Split(',').Select(s => int.Parse(s)).ToArray();
                        board.Rows.Add(rowData);
                    }
                }

                boardList.MultiBoards.Add(board);
            }

            var logoBoards = xmlDoc.Root.Elements("LogoBoard");
            foreach (var logoRows in logoBoards)
            {
                Logos logos = new Logos();
                logos.Name = logoRows.Attribute("Name")?.Value;
                logos.Rows = new List<LogoRowIdx>();

                var rowIdxs = logoRows.Elements("LogoRowIdx");
                foreach (var rowIdx in rowIdxs)
                {
                    LogoRowIdx logoRowIdx = new LogoRowIdx();
                    logoRowIdx.ID = rowIdx.Attribute("ID")?.Value; // ID 속성을 가져옴

                    var rowDataStr = rowIdx.Value;
                    if (!string.IsNullOrEmpty(rowDataStr))
                    {
                        var rowData = rowDataStr.Split(',').Select(s => int.Parse(s)).ToArray(); // 인덱스를 배열로 변환
                        logoRowIdx.Rows = rowData.ToList(); // 로우 인덱스를 담을 리스트로 변환하여 할당
                    }

                    logos.Rows.Add(logoRowIdx);
                }

                boardList.LogoBoard.Add(logos);
            }


            return boardList;
        }
    }
}
