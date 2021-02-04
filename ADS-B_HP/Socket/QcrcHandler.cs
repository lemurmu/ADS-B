using ADS_B_INFO;
using IFFOutPutApp;
using Mina.Core.Service;
using Mina.Core.Session;
using MyUnity;
using ReceiveDataProcess;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace MinaFilterTest
{
    public class QcrcHandler : IoHandlerAdapter
    {
        public override void ExceptionCaught(IoSession session, Exception cause)
        {
            Logger.Error(cause.Message);
        }

        private AutoResetEvent autoEvent = new AutoResetEvent(false);
        //private System.Threading.Timer timer;
        private const int MaxLength = 1888; //数据区的长度统一为1888个字节
        private byte[] _usualFrameHeadBuf = new byte[128];   //通用帧头都是128字节
        public UsualFrameHead _usualFrameHead = new UsualFrameHead();
        private static byte[] _headBuffer = new byte[32];
        public ADSB_IFF_Frame_Head _dataFrameHead = new ADSB_IFF_Frame_Head();
        private ConcurrentQueue<ClassificationResult> _ALLclassificationResultList = new ConcurrentQueue<ClassificationResult>();//所有IFF数据
        private Action<ClassificationResult> action;
        public Action<ClassificationResult> Action { get => action; set => action = value; }
        public AutoResetEvent AutoEvent { get => autoEvent; set => autoEvent = value; }
        bool isRuning = true;
        public bool IsRuning { get => isRuning; set => isRuning = value; }

        public override void SessionIdle(IoSession session, IdleStatus status)
        {
            isRuning = false;
            base.SessionIdle(session, status);
        }
        public override void MessageReceived(IoSession session, object message)
        {
            isRuning = true;
            QcrcRequestInfo info = message as QcrcRequestInfo;
          //  string msg = "";
            switch (info.Key)
            {
                case Globle.HEARTBEAT:
                  //  msg = nameof(Globle.HEARTBEAT);
                    break;
                case Globle.SELFCHECK:
                   // msg = nameof(Globle.SELFCHECK);
                    break;
                case Globle.UNSUALBEAT:
                  //  msg = nameof(Globle.UNSUALBEAT);
                    Array.Copy(info.Header, 0, _usualFrameHeadBuf, 4, info.Header.Length);
                    GetUsualFrameHead();
                    DataProcess.Process(info.Body, info.Body.Length);
                    DataProcess._usualFrameHead.Year = _usualFrameHead.Year;
                    if (_usualFrameHeadBuf[127] == 0x03)//ADS-B
                    {
                        if (_usualFrameHeadBuf[125] == 0x01 || _usualFrameHeadBuf[125] == 0x02)
                        {
                            Array.Copy(info.Body, 0, _headBuffer, 0, 32);
                            GetFrameHead();
                            if (_dataFrameHead.Flag == 0xAAAA)//识别结果
                            {
                                byte[] _dataADSBuffer = new byte[MaxLength];
                                Array.Copy(info.Body, 32, _dataADSBuffer, 0, info.Body.Length - 32);
                                DealIFFData(_dataADSBuffer);
                            }
                        }
                    }
                    break;
                default:
                    break;
            }
            // Console.WriteLine(msg);
        }

        public override void MessageSent(IoSession session, object message)
        {
            //Console.WriteLine("send.....");
        }

        public override void SessionClosed(IoSession session)
        {
            isRuning = false;
            IPEndPoint iPEnd = session.RemoteEndPoint as IPEndPoint;
            Logger.Info($"session Closed {iPEnd.Address}:{iPEnd.Port}");
        }

        public override void SessionCreated(IoSession session)
        {
            IPEndPoint iPEnd = session.RemoteEndPoint as IPEndPoint;
            Logger.Info($"session Created {iPEnd.Address}:{iPEnd.Port}");
        }

        public override void SessionOpened(IoSession session)
        {
            IPEndPoint iPEnd = session.RemoteEndPoint as IPEndPoint;
            Logger.Info($"session Opened {iPEnd.Address}:{iPEnd.Port}");
            //timer = new System.Threading.Timer(RefreshIffDataToGrid_Timertick, autoEvent, 0, 200);
        }

        private void DealIFFData(byte[] _dataADSBuffer)
        {
            for (int i = 0; i < 118; i++)
            {
                byte[] tmp1 = new byte[16];
                Buffer.BlockCopy(_dataADSBuffer, i * 16, tmp1, 0, 16);

                if (tmp1[0] == 0x55) //秒脉冲数据
                {
                    SecondPulseBlock spb;
                    GetSecondPulseBlock(tmp1, out spb);
                }
                else if (tmp1[0] == 0 && tmp1[1] == 0x00)    //说明是补0数据，不处理
                {
                    continue;
                }
                else //识别结果 block and 解调数据 block
                {
                    ClassificationResult rs;
                    GetClassification(tmp1, out rs);

                    if (rs.ChannelNo == 5 || rs.ChannelNo == 6 || rs.ChannelNo == 7 || rs.ChannelNo == 8)//俄制没有脉冲在此处过滤掉 sb设备报俄制的通道号
                        continue;


                    if (rs.ChannelNo == 2)//m5应答
                    {
                        i += 7;//m5固定跳8个block

                        byte[] demoData = new byte[112];
                        Array.Copy(_dataADSBuffer, i + 16, demoData, 0, demoData.Length);
                        if (demoData[0] == 0x00 && demoData[1] == 0x00 && demoData[2] == 0x00)
                            continue;
                        rs.DemoData = demoData;
                        //FilterModeData(rs);

                    }
                    else if (rs.ChannelNo == 4)// m5 询问
                    {
                        i += 7;

                        byte[] demoData = new byte[112];
                        Array.Copy(_dataADSBuffer, i + 16, demoData, 0, demoData.Length);
                        if (demoData[0] == 0x00 && demoData[1] == 0x00 && demoData[2] == 0x00)
                            continue;
                        rs.DemoData = demoData;//用户自己筛选22字节的解调数据 在7个block中是连续的
                        //FilterModeData(rs);
                    }
                    else
                    {
                        if (rs.Mode == 4 || rs.Mode == 6)//B D已经弃用
                            continue;
                    }

                    //m5不进
                    if (rs.DemoDataLength > 0 && rs.ChannelNo != 2 && rs.ChannelNo != 4 && ((1 <= rs.Mode && rs.Mode <= 18 && rs.Mode != 8)))   //说明后面还接有解调数据
                    {
                        int count = 0;
                        count = rs.DemoDataLength / 16;
                        int remained = rs.DemoDataLength % 16;
                        if (remained != 0)
                            count++;

                        rs.DemoData = new byte[rs.DemoDataLength];
                        int index = (i + 1) * 16;
                        if (rs.DemoDataLength <= _dataADSBuffer.Length - index + 1)//解决超出索引的错误
                        {
                            Array.Copy(_dataADSBuffer, index, rs.DemoData, 0, rs.DemoDataLength);
                        }
                        else
                        {
                            break;
                        }


                        if (count > 0)  //因为后面接解调数据，所以要跳过count个block
                            i += count;

                        // 数据解译
                        string additionalmessage = "";
                        // string airCode = "";
                        if (rs.Mode == 9 | rs.Mode == 10)//: 9  10   MarkS 询问 s模式16.25 30.25询问
                        {
                            //ProcessFunction.MarkSCDeciperHandle(rs.DemoData, rs.DemoDataLength, out additionalmessage);
                            //FilterModeData(rs);

                        }
                        else if (rs.Mode == 11)//  M-3/A应答	M3AS	21        M-C应答	MCAS	22                 MarkAC       Mark系列应答
                        {
                            if (rs.DemoData[0] == 0 && rs.DemoData[1] == 0)
                                continue;
                            byte[] newData = new byte[2];
                            newData[0] = (byte)(((rs.DemoData[0] & 0x70) >> 3) | ((rs.DemoData[0] & 0x07) >> 2));//01110111 01110111 底层报上来的数据格式!!转成00001111 11111111到库去解
                            newData[1] = (byte)((rs.DemoData[0] & 0x07 << 6) | ((rs.DemoData[1] & 0x70) >> 1) | (rs.DemoData[1] & 0x07));
                            rs.DemoData = newData;
                            //if ((rs.DemoData[0] & 0xF0) != 0)//高4bit有值，即数据超过12bit,产生误码
                            //{
                            //    rs.DemoData[0] = (byte)(rs.DemoData[0] & 0x0F);
                            //}
                            if (rs.info == null)
                                rs.info = new ADS_MessageInfo();
                            //rs.info.Height = (int)ProcessFunction.MarkACDeciperHandle(rs.Mode, rs.DemoData, rs.DemoDataLength, rs);
                            string[] info = ProcessFunction.ModeACHandle(rs.DemoData);
                            rs.Recoding = info[0];
                            rs.info.Height = Convert.ToInt32(info[1]);
                            if (rs.info.Height == 0)
                            {
                                rs.info = null;
                            }

                            //FilterModeData(rs);
                        }
                        else if (rs.Mode == 13) //13    MarkS    应答 56bit
                        {
                            rs = ProcessFunction.MarkSReceiveCDeciperHandle(rs, out additionalmessage);
                            //FilterModeData(rs);
                        }
                        else if (rs.Mode == 14)  // ADS-B	ADSB和S模式112bit共用14                                                ADS-B和S模式112bit
                        {
                            rs.info = ProcessFunction.ADSBDeciperHandle(rs.DemoData, rs.DemoDataLength, rs);
                            if (rs.info != null)
                            {
                                rs.Mode = 141;//ADS-B 141  用141来区分s112bit和ADS-B
                                FilterModeData(rs);
                                //FilterSector();
                            }
                            else//S模式112bit
                            {
                                rs = ProcessFunction.MarkSReceiveCDeciperHandle(rs, out additionalmessage);
                                //FilterModeData(rs);
                            }

                        }
                        else//有解调数据 但是没有库去解的 如m4
                        {
                            //FilterModeData(rs);

                        }

                    }
                    else//没有解调数据的
                    {
                        //FilterModeData(rs);

                    }
                }
            }
        }

        /// <summary>
        /// 数据融合
        /// </summary>
        /// <param name="rs"></param>
        /// <param name="_classificationResultList"></param>
        void FilterModeData(ClassificationResult rs)
        {
            if (_ALLclassificationResultList.Count > 100)//50条就释放
                ClearQueue();
            try
            {
                if (rs.Mode == 141)//ADS-B
                {
                    var matchResult = _ALLclassificationResultList.FirstOrDefault(r => r.info != null && r.info.ICAO == rs.info.ICAO && r.Mode == rs.Mode);
                    int msgType = ProcessFunction.GetMsgType(rs.DemoData);
                    if (matchResult == null)//集合中没有该模式和ICAO号的就添加
                    {
                        if (rs.info.ICAO == 0 && rs.Mode == 141)//扔掉没有ICAO号的ADS-B数据
                        {
                            return;
                        }
                        rs.MsgCount[msgType]++;
                        rs.Count++;
                        rs.TimeMark = rs.LocalDateTime;
                        rs.ReceiveTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                        rs.DataType = ClassDataType.New;
                        _ALLclassificationResultList.Enqueue(rs);
                        Action?.BeginInvoke(rs, null, null);
                    }
                    else
                    {
                        //更新相同模式相同ICAO的数据 空数据不替换 有新的数据才替换
                        if (matchResult.info != null)
                        {
                            if (!string.IsNullOrEmpty(rs.info.latitude))
                            {
                                matchResult.info.latitude = rs.info.latitude;
                                matchResult.DataType = ClassDataType.PrepareUpdate;
                            }

                            if (!string.IsNullOrEmpty(rs.info.longitude))
                            {
                                matchResult.info.longitude = rs.info.longitude;
                                matchResult.DataType = ClassDataType.PrepareUpdate;
                            }

                            if (!string.IsNullOrEmpty(rs.info.AirPlaneID))
                            {
                                matchResult.info.AirPlaneID = rs.info.AirPlaneID;
                                matchResult.DataType = ClassDataType.PrepareUpdate;
                            }

                            if (rs.info.AirSpeed != 0)
                            {
                                matchResult.info.AirSpeed = rs.info.AirSpeed;
                                matchResult.DataType = ClassDataType.PrepareUpdate;
                            }

                            if (rs.info.AirDirection != 0)
                            {
                                matchResult.info.AirDirection = rs.info.AirDirection;
                                matchResult.DataType = ClassDataType.PrepareUpdate;
                            }

                            if (rs.info.Height != 0)
                            {
                                matchResult.info.Height = rs.info.Height;
                                matchResult.DataType = ClassDataType.PrepareUpdate;
                            }

                            if (!string.IsNullOrEmpty(rs.TailNumber))
                            {
                                matchResult.TailNumber = rs.TailNumber;
                                matchResult.DataType = ClassDataType.PrepareUpdate;
                            }

                            if (!string.IsNullOrEmpty(rs.FightNumber))
                            {
                                matchResult.FightNumber = rs.FightNumber;
                                matchResult.DataType = ClassDataType.PrepareUpdate;
                            }

                            if (!string.IsNullOrEmpty(rs.Country))
                            {
                                matchResult.Country = rs.Country;
                                matchResult.DataType = ClassDataType.PrepareUpdate;
                            }

                            if (!string.IsNullOrEmpty(rs.PlaneType))
                            {
                                matchResult.PlaneType = rs.PlaneType;
                                matchResult.DataType = ClassDataType.PrepareUpdate;
                            }
                            if (!string.IsNullOrEmpty(rs.IdCode))
                            {
                                matchResult.IdCode = rs.IdCode;
                                matchResult.DataType = ClassDataType.PrepareUpdate;
                            }
                            if (!string.IsNullOrEmpty(rs.PlaneProperty))
                            {
                                matchResult.PlaneProperty = rs.PlaneProperty;
                                matchResult.DataType = ClassDataType.PrepareUpdate;
                            }

                            matchResult.MsgCount[msgType]++;
                            matchResult.Count++;
                            matchResult.info.ICAO = rs.info.ICAO;
                            matchResult.Mode = rs.Mode;
                            matchResult.DemoData = rs.DemoData;
                            matchResult.Freq = rs.Freq;
                            matchResult.TimeMark = rs.LocalDateTime;
                            matchResult.ReceiveTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                            if(matchResult.DataType!=ClassDataType.Old)
                                Action?.BeginInvoke(matchResult, null, null);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
            }
        }

        /// <summary>
        /// 定时上报和刷新显示数据
        /// </summary>
        /// <param name="sender"></param>
        //public void RefreshIffDataToGrid_Timertick(object sender)
        //{
        //    if (_ALLclassificationResultList.Count > 0)
        //    {
        //        //ADS-B s1 s2有ICAO的
        //        var ADS_BList = _ALLclassificationResultList.ToList();
        //        Action?.BeginInvoke(ADS_BList, null, null);
        //    }
        //}

        private void ClearQueue()
        {
            _ALLclassificationResultList = new ConcurrentQueue<ClassificationResult>();
        }
        private void GetClassification(byte[] data, out ClassificationResult rs)
        {
            rs = new ClassificationResult();
            if (data.Length != 16)
                return;
            rs.ChannelNo = (byte)(data[0] & 0x0F);
            rs.MainSideRatio = ((data[1] & 0x80) == 0) ? (1.0f / (data[1] & 0x7f)) : (1.0f * (data[1] & 0x7f));

            rs.PulseScope = BitConverter.ToUInt16(BigToSmallConvert(data, 4, 2), 0);
            // rs.Freq = BitConverter.ToInt16(data, 6) * 32 * 1000;// 换算
            // TOA

            rs.Second = (data[9] & 0x3f);
            rs.Minute = ((data[8] & 0x0f) << 2) + ((data[9] & 0xc0) >> 6);
            rs.Hour = ((data[8] & 0xf0) >> 4);
            if ((data[7] & 0x01) == 1)//下午
            {
                rs.Hour += 12;
            }
            rs.Day = ((data[7] & 0x3e) >> 1);
            rs.Month = ((data[7] & 0xc0) >> 6) + ((data[6] & 0x03) << 2);
            rs.Year = ((data[6] & 0xfc) >> 2);
            rs.SecondCount = (uint)(((BitConverter.ToUInt32(BigToSmallConvert(data, 10, 4), 0) * 1.0) / 112) * 1000); // 换算
            rs.Mode = data[14];

            rs.Freq = MyUnitConvert.GetFreqByMode(rs.Mode);
            rs.DemoDataLength = data[15];

        }

        //将16个字节解算成秒脉冲结构输出
        private void GetSecondPulseBlock(byte[] data, out SecondPulseBlock spb)
        {
            spb = new SecondPulseBlock();
            if (data.Length != 16)
                return;
            spb.Flag = data[0];
            spb.StartHour = Convert.ToByte(data[2] & 0xF0 >> 4);
            spb.StartMinute = Convert.ToByte(((data[2] & 0x0f) << 2) + (data[3] & 0xc0) >> 6);
            spb.StartSecond = Convert.ToByte(data[3] & 0x3f);

            spb.CurHour = Convert.ToByte(data[4] & 0xF0 >> 4);
            if ((data[3] & 0x01) == 1)//下午
            {
                spb.CurHour += 12;
            }
            spb.CurMinute = Convert.ToByte(((data[4] & 0x0f) << 2) + (data[5] & 0xc0) >> 6);
            spb.CurSecond = Convert.ToByte(data[5] & 0x3f);

            byte[] tmp = new byte[8];
            for (int i = 6; i < 12; i++)
                tmp[i - 6] = data[i];
            tmp[6] = 0;
            tmp[7] = 0;
            spb.MaxCount = BitConverter.ToUInt64(tmp, 0);
        }

        private void GetUsualFrameHead()
        {
            // todo 日志输出
            //-------------------------------依次解出通用帧头信息----------------------------------------
            _usualFrameHead.SyncHead = BitConverter.ToUInt32(_usualFrameHeadBuf, 0);
            _usualFrameHead.Year = _usualFrameHeadBuf[4];
            _usualFrameHead.BatchNo = _usualFrameHeadBuf[5];
            _usualFrameHead.CardNo = _usualFrameHeadBuf[6];
            _usualFrameHead.MainEditionNo = Convert.ToByte(((_usualFrameHeadBuf[7] & 0xF0) >> 4));
            _usualFrameHead.ViceEditonNo = Convert.ToByte(_usualFrameHeadBuf[7] & 0x0F);
            _usualFrameHead.PackSerialNo = BitConverter.ToUInt32(MyUnitConvert.BigToSmallConvert(_usualFrameHeadBuf, 8, 4), 0);
            ////////////////////////////////////////////////////////////////////////////////////////////////

            //_usualFrameHead.reserve = BitConverter.ToUInt16(BigToSmallConvert(_usualFrameHeadBuf,12,2), 0);
            _usualFrameHead.TOAType = _usualFrameHeadBuf[14];//0x00：为时标+秒内计数  0x01：为同步触发计数

            _usualFrameHead.second = (_usualFrameHeadBuf[19] & 0x3f);  //TOA[37:32]
            _usualFrameHead.minter = ((_usualFrameHeadBuf[18] & 0x0f) << 2) | ((_usualFrameHeadBuf[19] & 0xc0) >> 6);
            _usualFrameHead.hour = ((_usualFrameHeadBuf[18] & 0xf0) >> 4);
            _usualFrameHead.day = ((_usualFrameHeadBuf[17] & 0x3e) >> 1);
            if ((_usualFrameHeadBuf[17] & 0x01) == 1)//下午
            {
                _usualFrameHead.hour += 12;
            }

            _usualFrameHead.month = ((_usualFrameHeadBuf[17] & 0xc0) >> 6) + ((_usualFrameHeadBuf[16] & 0x03) << 2);
            _usualFrameHead.year = ((_usualFrameHeadBuf[16] & 0xfc) >> 2) + ((_usualFrameHeadBuf[15] & 0x01) << 6);
            _usualFrameHead.SecondCount = BitConverter.ToUInt32(MyUnitConvert.BigToSmallConvert(_usualFrameHeadBuf, 20, 4), 0) / 112;

            //_usualFrameHead.PayloadLength = BitConverter.ToUInt32(BigToSmallConvert(_usualFrameHeadBuf, 24, 4), 0);
            /////////////////////////////////////////////////////////////////////////////////////////////
            //_usualFrameHead.TM_Year = _usualFrameHeadBuf[12];
            //_usualFrameHead.TM_Day = BitConverter.ToInt16(_usualFrameHeadBuf, 13);
            //_usualFrameHead.TM_Hour = _usualFrameHeadBuf[15];
            //_usualFrameHead.TM_Minute = _usualFrameHeadBuf[16];
            //_usualFrameHead.TM_Second = _usualFrameHeadBuf[17];
            //_usualFrameHead.SecondCount = BitConverter.ToUInt32(_usualFrameHeadBuf, 18);
            //_usualFrameHead.PayloadLength = BitConverter.ToUInt32(_usualFrameHeadBuf, 22);

            // 通用侦头 todo 
            _usualFrameHead.retain_DeviceCheck = _usualFrameHeadBuf.Skip(26).Take(2).ToArray();
            for (int i = 0; i < 6; i++)
            {
                _usualFrameHead.RF_Freq[i] = BitConverter.ToUInt32(MyUnitConvert.BigToSmallConvert(_usualFrameHeadBuf, 29 + i * 8, 4), 0) * 1000;
                _usualFrameHead.RF_Diminish[i] = _usualFrameHeadBuf[33 + i * 8];
                _usualFrameHead.RF_Bandwidth[i] = _usualFrameHeadBuf[34 + i * 8];
                _usualFrameHead.RF_Wordmode[i] = _usualFrameHeadBuf[35 + i * 8];
            }

            //经纬度 计算
            _usualFrameHead.Latitude = ((float)((_usualFrameHeadBuf[81] & 0xf0) >> 4) * 1000 + (_usualFrameHeadBuf[81] & 0x0f) * 100 + (_usualFrameHeadBuf[82] & 0xf0 >> 4) * 10 + (_usualFrameHeadBuf[82] & 0x0f >> 4) + (float)(_usualFrameHeadBuf[83] & 0x0f) / 10 + (float)((_usualFrameHeadBuf[84] & 0xf0) >> 4) / 100 + (float)(_usualFrameHeadBuf[84] & 0x0f) / 1000 + (float)((_usualFrameHeadBuf[85] & 0xf0) >> 4) / 10000 + (float)(_usualFrameHeadBuf[85] & 0x0f) / 100000 + (float)((_usualFrameHeadBuf[86] & 0xf0) >> 4) / 1000000 + (float)(_usualFrameHeadBuf[86] & 0x0f) / 10000000) / 100;
            _usualFrameHead.Longitude = ((float)(
                (_usualFrameHeadBuf[88] & 0x0f) * 10000
                + ((_usualFrameHeadBuf[89] & 0xf0) >> 4) * 1000
                + (_usualFrameHeadBuf[89] & 0x0f) * 100
                + ((_usualFrameHeadBuf[90] & 0xF0) >> 4) * 10
                + (_usualFrameHeadBuf[90] & 0x0f))
                + (_usualFrameHeadBuf[91] & 0x0f) / 10.0f
                + ((_usualFrameHeadBuf[92] & 0xf0) >> 4) / 100.0f
                + (_usualFrameHeadBuf[92] & 0x0f) / 1000.0f +
                ((_usualFrameHeadBuf[93] & 0xf0) >> 4) / 10000.0f
                + (_usualFrameHeadBuf[93] & 0x0f) / 100000.0f
                + ((_usualFrameHeadBuf[94] & 0xf0) >> 4) / 1000000.0f
                + (_usualFrameHeadBuf[94] & 0x0f) / 10000000.0f
                ) / 100.0f;
            _usualFrameHead.DataType = _usualFrameHeadBuf[125];
            _usualFrameHead.Channel = _usualFrameHeadBuf[126];
            _usualFrameHead.DataFormat = _usualFrameHeadBuf[127];
            //-------------------------------通用帧头信息解算完毕---------------------------------------

        }

        public void GetFrameHead()
        {
            _dataFrameHead.Flag = BitConverter.ToUInt16(_headBuffer, 0);
            _dataFrameHead.Freq_12 = BitConverter.ToUInt16(BigToSmallConvert(_headBuffer, 2, 2), 0) * 32 * 1000;
            _dataFrameHead.Freq_34 = BitConverter.ToUInt16(BigToSmallConvert(_headBuffer, 4, 2), 0) * 32 * 1000;
            _dataFrameHead.Freq_5 = BitConverter.ToUInt16(BigToSmallConvert(_headBuffer, 6, 2), 0) * 32 * 1000;
            _dataFrameHead.Freq_6 = BitConverter.ToUInt16(BigToSmallConvert(_headBuffer, 8, 2), 0) * 32 * 1000;
            _dataFrameHead.Freq_7 = BitConverter.ToUInt16(BigToSmallConvert(_headBuffer, 10, 2), 0) * 32 * 1000;
            _dataFrameHead.Freq_8 = BitConverter.ToUInt16(BigToSmallConvert(_headBuffer, 12, 2), 0) * 32 * 1000;
            _dataFrameHead.ChannelGate = BitConverter.ToUInt16(BigToSmallConvert(_headBuffer, 14, 2), 0);

            _dataFrameHead.Second = (_headBuffer[21] & 0x3f);
            _dataFrameHead.Minute = ((_headBuffer[20] & 0x0f) << 2) | ((_headBuffer[21] & 0xc0) >> 6);
            _dataFrameHead.Hour = ((_headBuffer[20] & 0xf0) >> 4);
            _dataFrameHead.Day = ((_headBuffer[19] & 0x3e) >> 1);
            _dataFrameHead.Month = ((_headBuffer[19] & 0xc0) >> 6) + ((_headBuffer[18] & 0x03) << 2);
            _dataFrameHead.Year = ((_headBuffer[18] & 0xfc) >> 2) + ((_headBuffer[17] & 0x01) << 6);
            _dataFrameHead.SerialNo = BitConverter.ToUInt32(BigToSmallConvert(_headBuffer, 22, 4), 0);

        }

        static byte[] BigToSmallConvert(byte[] input, int index, int length)
        {
            byte[] output = new byte[length];
            for (int i = 0; i < length; i++)
            {
                output[length - i - 1] = input[i + index];
            }
            return output;
        }
    }
}
