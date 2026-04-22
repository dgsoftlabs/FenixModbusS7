using ProjectDataLib;
using Sharp7;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading;

namespace nmDriver
{
    public class Driver : IDriverModel, IDisposable
    {
        /// <summary>
        /// Konstruktor
        /// </summary>
        public Driver()
        {
            //Parametry Tramsmisji
            DriverParam_ = new S7DriverParam();

            ObjId_ = Guid.NewGuid();

            //Obsluga Backgriundworker
            bWorker = new BackgroundWorker();
            bWorker.WorkerSupportsCancellation = true;
            bWorker.DoWork += new DoWorkEventHandler(bWorker_DoWork);
            bWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bWorker_RunWorkerCompleted);
        }

        private S7DriverParam DriverParam_;
        private Boolean isLive = false;
        private static readonly S7Client dc = new S7Client();
        private static readonly object commLock = new object();

        private List<Tag> tagList = new List<Tag>();
        private List<PO> MPoints = new List<PO>();
        private List<PO> IPoints = new List<PO>();
        private List<PO> QPoints = new List<PO>();
        private List<List<PO>> DBPoints = new List<List<PO>>();

        private static string M = "M";
        private static string DB = "DB";
        private static string I = "I";
        private static string Q = "Q";

        private EventHandler sendInfoEv;
        private EventHandler errorSendEv;

        private EventHandler refreshedPartial;
        private EventHandler refreshCycleEv;

        private EventHandler sendLogInfoEv;
        private EventHandler reciveLogInfoEv;

        //Beckgroundworker
        [NonSerialized]
        private
        //Watek wtle
        BackgroundWorker bWorker;

        private Guid ObjId_;

        Guid IDriverModel.ObjId
        {
            get { return ObjId_; }
        }

        /// <summary>
        /// Nazwa sterownika
        /// </summary>
        string IDriverModel.driverName
        {
            get { return "S7-300-400 Ethernet"; }
        }

        /// <summary>
        /// Parametry sterownika
        /// </summary>
        object IDriverModel.setDriverParam
        {
            get
            {
                return DriverParam_;
            }
            set
            {
                DriverParam_ = (S7DriverParam)value;
            }
        }

        /// <summary>
        /// Aktywacja cyklu
        /// </summary>
        /// <param name="tagsList"></param>
        /// <returns></returns>
        bool IDriverModel.activateCycle(List<ITag> tagsList2)
        {
            try
            {
                if (bWorker.IsBusy)
                    return false;

                this.tagList.Clear();
                this.tagList = (from c in tagsList2 where c is Tag select (Tag)c).ToList();

                ConfigData(this.tagList);

                int connResult = ConnectWithRetry();
                if (connResult == 0)
                {
                    bWorker.RunWorkerAsync();
                    isLive = true;

                    foreach (IDriverModel it in this.tagList)
                        it.isAlive = true;

                    sendInfoEv?.Invoke(this, new ProjectEventArgs(DateTime.Now, "S7 connected: " + DriverParam_.Ip + ":" + DriverParam_.Port + " CPU=" + DriverParam_.CpuSeries));
                    return true;
                }

                errorSendEv?.Invoke(this, new ProjectEventArgs(new byte[] { 0 }, DateTime.Now,
                    "Couldn't open TCP connection. code=" + connResult + " " + dc.ErrorText(connResult)));
                return false;
            }
            catch (Exception Ex)
            {
                errorSendEv?.Invoke(this, new ProjectEventArgs(new byte[] { 0 }, DateTime.Now, Ex.Message, Ex));
                return false;
            }
        }

        /// <summary>
        /// Dodanie tagow
        /// </summary>
        /// <param name="tagList"></param>
        /// <returns></returns>
        bool IDriverModel.addTagsComm(List<ITag> tagList)
        {
            try
            {
                List<Tag> tgs = (from c in tagList select (Tag)c).ToList();

                //Dodanie Tagow
                this.tagList.AddRange(tgs);

                //Rekonfiguracja
                ConfigData(this.tagList);

                return true;
            }
            catch (Exception Ex)
            {
                if (errorSendEv != null)
                    errorSendEv(this, new ProjectEventArgs(new byte[] { 0 }, DateTime.Now, Ex.Message, Ex));

                return false;
            }
        }

        /// <summary>
        /// Usuniecie Tagow
        /// </summary>
        /// <param name="tagList"></param>
        /// <returns></returns>
        bool IDriverModel.removeTagsComm(List<ITag> tagList2)
        {
            try
            {
                List<Tag> tgs = (from c in tagList2 select (Tag)c).ToList();

                foreach (Tag tn in tgs)
                    tagList.Remove(tn);

                //Rekonfiguracja
                ConfigData(this.tagList);

                //
                return true;
            }
            catch (Exception Ex)
            {
                if (errorSendEv != null)
                    errorSendEv(this, new ProjectEventArgs(new byte[] { 0 }, DateTime.Now, Ex.Message, Ex));

                return false;
            }
        }

        /// <summary>
        /// Rekonfiguruj dane
        /// </summary>
        /// <returns></returns>
        bool IDriverModel.reConfig()
        {
            try
            {
                ConfigData(this.tagList);

                return true;
            }
            catch (Exception Ex)
            {
                if (errorSendEv != null)
                    errorSendEv(this, new ProjectEventArgs(new byte[] { 0 }, DateTime.Now, Ex.Message, Ex));

                return false;
            }
        }

        /// <summary>
        /// Deaktywacja cyklu
        /// </summary>
        /// <returns></returns>
        bool IDriverModel.deactivateCycle()
        {
            try
            {
                if (isLive)
                {
                    isLive = false;

                    lock (commLock)
                    {
                        if (dc.Connected)
                            dc.Disconnect();
                    }

                    foreach (IDriverModel it in this.tagList)
                        it.isAlive = false;
                }

                return true;
            }
            catch (Exception Ex)
            {
                errorSendEv?.Invoke(this, new ProjectEventArgs(new byte[] { 0 }, DateTime.Now, Ex.Message, Ex));
                return false;
            }
        }

        /// <summary>
        /// Głowna praca sterownika - pętla obiegowa
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void bWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                if (!isLive)
                    return;

                if (!EnsureConnected())
                    return;

                ReadConfiguredArea(MPoints, S7Consts.S7AreaMK, 0, 1, "M");

                foreach (List<PO> pp in DBPoints)
                    ReadConfiguredArea(pp, S7Consts.S7AreaDB, 0, 4, "DB", true);

                ReadConfiguredArea(IPoints, S7Consts.S7AreaPE, 0, 2, "I");
                ReadConfiguredArea(QPoints, S7Consts.S7AreaPA, 0, 3, "Q");

                WriteModifiedTags();
            }
            catch (Exception Ex)
            {
                errorSendEv?.Invoke(this, new ProjectEventArgs(new byte[] { 0 }, DateTime.Now, "S7-300/400.bWorker_DoWork :" + Ex.Message, Ex));
            }

            Thread.Sleep(DriverParam_.ReplyTime);
        }

        /// <summary>
        /// Praca wykonana i przejscie do nastpnej albo zatrzymanie
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void bWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            try
            {
                if (isLive)
                {
                    bWorker.RunWorkerAsync();
                }
                else
                {
                    lock (commLock)
                    {
                        if (dc.Connected)
                            dc.Disconnect();
                    }
                }
            }
            catch (Exception Ex)
            {
                errorSendEv?.Invoke(this, new ProjectEventArgs(new byte[] { 0 }, DateTime.Now, "S7-300/400.bWorker_RunWorkerCompleted :" + Ex.Message, Ex));
            }
        }

        private bool EnsureConnected()
        {
            lock (commLock)
            {
                if (dc.Connected)
                    return true;
            }

            int result = ConnectWithRetry();
            if (result != 0)
            {
                errorSendEv?.Invoke(this, new ProjectEventArgs(new byte[] { 0 }, DateTime.Now,
                    "Reconnect failed. code=" + result + " " + dc.ErrorText(result)));
                return false;
            }

            sendInfoEv?.Invoke(this, new ProjectEventArgs(DateTime.Now, "Reconnect successful."));
            return true;
        }

        private int ConnectWithRetry()
        {
            int attempts = DriverParam_.ReconnectAttempts;
            int lastResult = 0;

            for (int i = 0; i < attempts; i++)
            {
                lastResult = ConnectOnce();
                if (lastResult == 0)
                    return 0;

                Thread.Sleep(200);
            }

            return lastResult;
        }

        private int ConnectOnce()
        {
            lock (commLock)
            {
                if (dc.Connected)
                    dc.Disconnect();

                int timeout = DriverParam_.Timeout < 1 ? 1 : DriverParam_.Timeout;
                dc.ConnTimeout = timeout;
                dc.RecvTimeout = timeout;
                dc.SendTimeout = timeout;

                int result = dc.ConnectTo(DriverParam_.Ip, DriverParam_.Rack, DriverParam_.Slot);
                if (result == 0)
                    return 0;

                if (DriverParam_.CpuSeries == S7CpuSeries.S7400 && DriverParam_.Slot != 3)
                    return dc.ConnectTo(DriverParam_.Ip, DriverParam_.Rack, 3);

                if (DriverParam_.CpuSeries == S7CpuSeries.S7300 && DriverParam_.Slot != 2)
                    return dc.ConnectTo(DriverParam_.Ip, DriverParam_.Rack, 2);

                return result;
            }
        }

        private void ApplyOperationTimeout(int timeout)
        {
            int effectiveTimeout = timeout < 1 ? 1 : timeout;
            lock (commLock)
            {
                dc.RecvTimeout = effectiveTimeout;
                dc.SendTimeout = effectiveTimeout;
            }
        }

        private void ReadConfiguredArea(List<PO> points, int area, int blockNum, int fctCode, string areaName, bool usePointBlock = false)
        {
            if (points == null || points.Count == 0)
                return;

            MemoryAreaInfo mInfo = ((IDriverModel)this).MemoryAreaInf.First(x => x.fctCode == fctCode);

            foreach (PO p in points)
            {
                int size = (p.Y - p.X) + 1;
                byte[] data = new byte[size];
                int dbNumber = usePointBlock ? p.BlockNum : blockNum;

                sendLogInfoEv?.Invoke(this, new ProjectEventArgs(new byte[] { (byte)area }, DateTime.Now, areaName + ".READ.REQUEST"));

                ApplyOperationTimeout(DriverParam_.ReadTimeout);

                int readResult;
                lock (commLock)
                {
                    readResult = dc.ReadArea(area, dbNumber, p.X, size, S7Consts.S7WLByte, data);
                }

                if (readResult == S7Consts.ResultOK)
                {
                    BitArray btArr = new BitArray(data);

                    reciveLogInfoEv?.Invoke(this, new ProjectEventArgs(data, DateTime.Now, areaName + ".READ.RESPONSE"));

                    Boolean[] bitArray = new Boolean[btArr.Length];
                    btArr.CopyTo(bitArray, 0);

                    var zm = from n in tagList
                             where n.areaData == mInfo.Name
                             && (!usePointBlock || n.BlockAdress == p.BlockNum)
                             && n.bitAdres >= p.X * mInfo.AdresSize
                             && n.bitAdres + n.coreData.Length <= (p.X + size) * mInfo.AdresSize
                             select n;

                    List<Tag> refDataTag = zm.ToList();

                    for (int i = 0; i < refDataTag.Count; i++)
                    {
                        Array.Copy(bitArray, refDataTag[i].bitAdres - p.X * mInfo.AdresSize, refDataTag[i].coreData, 0, refDataTag[i].coreData.Length);
                        refDataTag[i].refreshData();
                    }

                    if (refDataTag.Count > 0)
                        refreshCycleEv?.Invoke(this, new ProjectEventArgs(refDataTag));
                }
                else
                {
                    errorSendEv?.Invoke(this, new ProjectEventArgs(new byte[] { 0 }, DateTime.Now,
                        "READ ERROR area=" + areaName + " block=" + dbNumber + " start=" + p.X + " size=" + size + " code=" + readResult + " " + dc.ErrorText(readResult)));

                    if (readResult == S7Consts.errTCPNotConnected || readResult == S7Consts.errTCPConnectionReset || readResult == S7Consts.errTCPConnectionTimeout)
                        EnsureConnected();
                }
            }
        }

        private void WriteModifiedTags()
        {
            List<Tag> tgWrite = tagList.Where(x => x.setMod).ToList();
            if (tgWrite.Count == 0)
                return;

            foreach (Tag tg in tgWrite)
            {
                try
                {
                    BitArray btArray = new BitArray(tg.coreDataSend);
                    int byteLen = Math.Max(1, (tg.coreDataSend.Length + 7) / 8);
                    byte[] buffByteArr = new byte[byteLen];
                    btArray.CopyTo(buffByteArr, 0);

                    int area;
                    int dbNumber;
                    string logArea;

                    if (tg.areaData == M)
                    {
                        area = S7Consts.S7AreaMK;
                        dbNumber = 0;
                        logArea = "M";
                    }
                    else if (tg.areaData == DB)
                    {
                        area = S7Consts.S7AreaDB;
                        dbNumber = tg.BlockAdress;
                        logArea = "DB";
                    }
                    else if (tg.areaData == I)
                    {
                        area = S7Consts.S7AreaPE;
                        dbNumber = 0;
                        logArea = "I";
                    }
                    else if (tg.areaData == Q)
                    {
                        area = S7Consts.S7AreaPA;
                        dbNumber = 0;
                        logArea = "Q";
                    }
                    else
                    {
                        continue;
                    }

                    ApplyOperationTimeout(DriverParam_.WriteTimeout);

                    int writeResult;
                    lock (commLock)
                    {
                        writeResult = dc.WriteArea(area, dbNumber, tg.startData, byteLen, S7Consts.S7WLByte, buffByteArr);
                    }

                    if (writeResult == S7Consts.ResultOK)
                    {
                        tg.setMod = false;
                        sendLogInfoEv?.Invoke(this, new ProjectEventArgs(new byte[] { (byte)area }, DateTime.Now, logArea + ".WRITE.REQUEST"));
                    }
                    else
                    {
                        errorSendEv?.Invoke(this, new ProjectEventArgs(new byte[] { 0 }, DateTime.Now,
                            "WRITE ERROR area=" + logArea + " block=" + dbNumber + " start=" + tg.startData + " size=" + byteLen + " code=" + writeResult + " " + dc.ErrorText(writeResult)));

                        if (writeResult == S7Consts.errTCPNotConnected || writeResult == S7Consts.errTCPConnectionReset || writeResult == S7Consts.errTCPConnectionTimeout)
                            EnsureConnected();
                    }
                }
                catch (Exception Ex)
                {
                    errorSendEv?.Invoke(this, new ProjectEventArgs(new byte[] { 0 }, DateTime.Now, "SiemensS7.bWorker_DoWork.Write :" + Ex.Message));
                }
            }
        }

        /// <summary>
        /// Konfiguracja parmamertów Bufforów dla rejestrow
        /// </summary>
        /// <param name="tagList"></param>
        private void ConfigData(List<Tag> tagList)
        {
            try
            {
                if (tagList == null)
                    return;

                getFrameSizeM(tagList, M, 230 * 8, 130);
                getFrameSizeI(tagList, I, 230 * 8, 130);
                getFrameSizeQ(tagList, Q, 230 * 8, 130);
                getFrameSizeDB(tagList, DB, 230 * 8, 130);
            }
            catch (Exception Ex)
            {
                if (errorSendEv != null)
                    errorSendEv(this, new ProjectEventArgs(new byte[] { 0 }, DateTime.Now, "S7-300/400.configSingleDevice.CO :" + Ex.Message, Ex));
            }
        }

        /// <summary>
        /// Pobranie rozmiarw elementow do pobrania
        /// </summary>
        /// <param name="tags"></param>
        /// <param name="type"></param>
        /// <param name="maxFrSize"></param>
        /// <param name="maxSize"></param>
        /// <returns></returns>
        private void getFrameSizeM(List<Tag> tags, string type, int maxFrSize, int maxSize)
        {
            try
            {
                MPoints.Clear();

                //Pobranie odpowiednich typow tagow
                var tgs = (from t in tags where t.areaData == type select t).ToList();

                if (tgs.Count == 0)
                    return;

                //Najnizszy i najwyzszy adres tagowy.
                int min = tgs.Min(x => x.startData);
                int max = tgs.Max(x => (x.startData + x.coreData.Length / 8)) - 1;
                int marker = 0;

                //Dodanie buffora na punkty
                List<PO> points = new List<PO>();
                points.Add(new PO(min, 0));

                //Petla obiegowa
                for (int i = min; i <= max; i++)
                {
                    #region Zabezpiecznie dlugiej ramki

                    if (points[points.Count - 1].Y - points[points.Count - 1].X >= maxFrSize)
                    {
                        //Jestemy w miejscu Taga trzeba podac koniec
                        Tag halfTag = tgs.Where(x => x.startData <= i && (x.startData + x.coreData.Length / 8) > i).ToList()[0];
                        if (halfTag != null)
                        {
                            points[points.Count - 1].Y = halfTag.startData + halfTag.coreData.Length / 8 - 1;
                            i = points[points.Count - 1].Y + 1;
                            points.Add(new PO(i, 0));
                        }
                        else
                            points.Add(new PO(i, 0));
                    }

                    #endregion Zabezpiecznie dlugiej ramki

                    #region Zabezpieczenie przed przekroczeniem wolnego odstepu

                    if (marker == maxSize)
                        points.Add(new PO(i, 0));

                    #endregion Zabezpieczenie przed przekroczeniem wolnego odstepu

                    #region Sprawdzenie czy tagi sa w zakresie

                    if (tgs.Where(x => x.startData <= i && (x.startData + x.coreData.Length / 8) > i).ToList().Count > 0)
                    {
                        //DANIE
                        points[points.Count - 1].Y = i;

                        //Dociagnicie do adresu po powrocie do odliczania
                        if (marker >= maxSize)
                            points[points.Count - 1].X = i;

                        marker = 0;
                    }
                    else
                    {
                        //Przesuwaj X
                        if (marker >= maxSize)
                            points[points.Count - 1].X = i;

                        //Inkrementuj marker
                        marker++;
                    }

                    #endregion Sprawdzenie czy tagi sa w zakresie
                }

                MPoints.AddRange(points);
                return;
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Pobranie rozmiarw elementow do pobrania
        /// </summary>
        /// <param name="tags"></param>
        /// <param name="type"></param>
        /// <param name="maxFrSize"></param>
        /// <param name="maxSize"></param>
        /// <returns></returns>
        private void getFrameSizeI(List<Tag> tags, string type, int maxFrSize, int maxSize)
        {
            try
            {
                IPoints.Clear();

                //Pobranie odpowiednich typow tagow
                var tgs = (from t in tags where t.areaData == type select t).ToList();

                if (tgs.Count == 0)
                    return;

                //Najnizszy i najwyzszy adres tagowy.
                int min = tgs.Min(x => x.startData);
                int max = tgs.Max(x => (x.startData + x.coreData.Length / 8)) - 1;
                int marker = 0;

                //Dodanie buffora na punkty
                List<PO> points = new List<PO>();
                points.Add(new PO(min, 0));

                //Petla obiegowa
                for (int i = min; i <= max; i++)
                {
                    #region Zabezpiecznie dlugiej ramki

                    if (points[points.Count - 1].Y - points[points.Count - 1].X >= maxFrSize)
                    {
                        //Jestemy w miejscu Taga trzeba podac koniec
                        Tag halfTag = tgs.Where(x => x.startData <= i && (x.startData + x.coreData.Length / 8) > i).ToList()[0];
                        if (halfTag != null)
                        {
                            points[points.Count - 1].Y = halfTag.startData + halfTag.coreData.Length / 8 - 1;
                            i = points[points.Count - 1].Y + 1;
                            points.Add(new PO(i, 0));
                        }
                        else
                            points.Add(new PO(i, 0));
                    }

                    #endregion Zabezpiecznie dlugiej ramki

                    #region Zabezpieczenie przed przekroczeniem wolnego odstepu

                    if (marker == maxSize)
                        points.Add(new PO(i, 0));

                    #endregion Zabezpieczenie przed przekroczeniem wolnego odstepu

                    #region Sprawdzenie czy tagi sa w zakresie

                    if (tgs.Where(x => x.startData <= i && (x.startData + x.coreData.Length / 8) > i).ToList().Count > 0)
                    {
                        //DANIE
                        points[points.Count - 1].Y = i;

                        //Dociagnicie do adresu po powrocie do odliczania
                        if (marker >= maxSize)
                            points[points.Count - 1].X = i;

                        marker = 0;
                    }
                    else
                    {
                        //Przesuwaj X
                        if (marker >= maxSize)
                            points[points.Count - 1].X = i;

                        //Inkrementuj marker
                        marker++;
                    }

                    #endregion Sprawdzenie czy tagi sa w zakresie
                }

                IPoints.AddRange(points);
                return;
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Pobranie rozmiarw elementow do pobrania
        /// </summary>
        /// <param name="tags"></param>
        /// <param name="type"></param>
        /// <param name="maxFrSize"></param>
        /// <param name="maxSize"></param>
        /// <returns></returns>
        private void getFrameSizeQ(List<Tag> tags, string type, int maxFrSize, int maxSize)
        {
            try
            {
                QPoints.Clear();

                //Pobranie odpowiednich typow tagow
                var tgs = (from t in tags where t.areaData == type select t).ToList();

                if (tgs.Count == 0)
                    return;

                //Najnizszy i najwyzszy adres tagowy.
                int min = tgs.Min(x => x.startData);
                int max = tgs.Max(x => (x.startData + x.coreData.Length / 8)) - 1;
                int marker = 0;

                //Dodanie buffora na punkty
                List<PO> points = new List<PO>();
                points.Add(new PO(min, 0));

                //Petla obiegowa
                for (int i = min; i <= max; i++)
                {
                    #region Zabezpiecznie dlugiej ramki

                    if (points[points.Count - 1].Y - points[points.Count - 1].X >= maxFrSize)
                    {
                        //Jestemy w miejscu Taga trzeba podac koniec
                        Tag halfTag = tgs.Where(x => x.startData <= i && (x.startData + x.coreData.Length / 8) > i).ToList()[0];
                        if (halfTag != null)
                        {
                            points[points.Count - 1].Y = halfTag.startData + halfTag.coreData.Length / 8 - 1;
                            i = points[points.Count - 1].Y + 1;
                            points.Add(new PO(i, 0));
                        }
                        else
                            points.Add(new PO(i, 0));
                    }

                    #endregion Zabezpiecznie dlugiej ramki

                    #region Zabezpieczenie przed przekroczeniem wolnego odstepu

                    if (marker == maxSize)
                        points.Add(new PO(i, 0));

                    #endregion Zabezpieczenie przed przekroczeniem wolnego odstepu

                    #region Sprawdzenie czy tagi sa w zakresie

                    if (tgs.Where(x => x.startData <= i && (x.startData + x.coreData.Length / 8) > i).ToList().Count > 0)
                    {
                        //DANIE
                        points[points.Count - 1].Y = i;

                        //Dociagnicie do adresu po powrocie do odliczania
                        if (marker >= maxSize)
                            points[points.Count - 1].X = i;

                        marker = 0;
                    }
                    else
                    {
                        //Przesuwaj X
                        if (marker >= maxSize)
                            points[points.Count - 1].X = i;

                        //Inkrementuj marker
                        marker++;
                    }

                    #endregion Sprawdzenie czy tagi sa w zakresie
                }

                QPoints.AddRange(points);
                return;
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Pobranie rozmiarw elementow do pobrania
        /// </summary>
        /// <param name="tags"></param>
        /// <param name="type"></param>
        /// <param name="maxFrSize"></param>
        /// <param name="maxSize"></param>
        /// <returns></returns>
        private void getFrameSizeDB(List<Tag> tags, string type, int maxFrSize, int maxSize)
        {
            try
            {
                DBPoints.Clear();
                IEnumerable<IGrouping<int, Tag>> tagi = tags.Where(x => x.areaData == type).GroupBy(x => x.BlockAdress, x => x).ToList();
                foreach (IGrouping<int, Tag> xx in tagi)
                {
                    //Pobranie odpowiednich typow tagow
                    var tgs = from t in xx select t;

                    //Najnizszy i najwyzszy adres tagowy.
                    int min = tgs.Min(x => x.startData);
                    int max = tgs.Max(x => (x.startData + x.coreData.Length / 8)) - 1;
                    int marker = 0;

                    List<PO> points = new List<PO>();
                    points.Add(new PO(min, 0));
                    points.Last().BlockNum = tgs.Last().BlockAdress;

                    //Petla obiegowa
                    for (int i = min; i <= max; i++)
                    {
                        #region Zabezpiecznie dlugiej ramki

                        if (points[points.Count - 1].Y - points[points.Count - 1].X >= maxFrSize)
                        {
                            //Jestemy w miejscu Taga trzeba podac koniec
                            Tag halfTag = tgs.Where(x => x.startData <= i && (x.startData + x.coreData.Length / 8) > i).ToList()[0];
                            if (halfTag != null)
                            {
                                points[points.Count - 1].Y = halfTag.startData + halfTag.coreData.Length / 8 - 1;
                                i = points[points.Count - 1].Y + 1;
                                points.Add(new PO(i, 0));
                            }
                            else
                                points.Add(new PO(i, 0));
                        }

                        #endregion Zabezpiecznie dlugiej ramki

                        #region Zabezpieczenie przed przekroczeniem wolnego odstepu

                        if (marker == maxSize)
                            points.Add(new PO(i, 0));

                        #endregion Zabezpieczenie przed przekroczeniem wolnego odstepu

                        #region Sprawdzenie czy tagi sa w zakresie

                        if (tgs.Where(x => x.startData <= i && (x.startData + x.coreData.Length / 8) > i).ToList().Count > 0)
                        {
                            //DANIE
                            points[points.Count - 1].Y = i;

                            //Dociagnicie do adresu po powrocie do odliczania
                            if (marker >= maxSize)
                                points[points.Count - 1].X = i;

                            marker = 0;
                        }
                        else
                        {
                            //Przesuwaj X
                            if (marker >= maxSize)
                                points[points.Count - 1].X = i;

                            //Inkrementuj marker
                            marker++;
                        }

                        #endregion Sprawdzenie czy tagi sa w zakresie
                    }

                    DBPoints.Add(points);
                }

                return;
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Odswierzenie danych
        /// </summary>
        event EventHandler IDriverModel.refreshedCycle
        {
            add { refreshCycleEv += value; }
            remove { refreshCycleEv -= value; }
        }

        /// <summary>
        /// Odswierzenie częsciowe
        /// </summary>
        event EventHandler IDriverModel.refreshedPartial
        {
            add { refreshedPartial += value; }
            remove { refreshedPartial -= value; }
        }

        /// <summary>
        /// Dane wychodzące ze sterownika
        /// </summary>
        event EventHandler IDriverModel.dataSent
        {
            add { sendLogInfoEv += value; }
            remove { sendLogInfoEv -= value; }
        }

        /// <summary>
        /// dane przychodzoce do sterownika
        /// </summary>
        event EventHandler IDriverModel.dataRecived
        {
            add { reciveLogInfoEv += value; }
            remove { reciveLogInfoEv -= value; }
        }

        /// <summary>
        /// Informacje o blędach
        /// </summary>
        event EventHandler IDriverModel.error
        {
            add { errorSendEv += value; }
            remove { errorSendEv -= value; }
        }

        /// <summary>
        /// Inne informacje z klasy
        /// </summary>
        event EventHandler IDriverModel.information
        {
            add { sendInfoEv += value; }
            remove { sendInfoEv -= value; }
        }

        /// <summary>
        /// Rodzaje rejestrw
        /// </summary>
        MemoryAreaInfo[] IDriverModel.MemoryAreaInf
        {
            get
            {
                return new MemoryAreaInfo[] {
                    new MemoryAreaInfo(M,8,1),
                    new MemoryAreaInfo(I,8,2),
                    new MemoryAreaInfo(Q,8,3),
                    new MemoryAreaInfo(DB,8,4)
                };
            }
        }

        /// <summary>
        /// Request
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="num"></param>
        /// <returns></returns>
        string IDriverModel.FormatFrameRequest(byte[] frame, System.Globalization.NumberStyles num)
        {
            if (frame == null)
                return "Empty reponse";

            switch (num)
            {
                case NumberStyles.HexNumber:
                    return frame.Aggregate("", (a, e) => a + e.ToString("X"));

                case NumberStyles.Integer:
                    return frame.Aggregate("", (a, e) => a + e.ToString());

                default:
                    return "Internal problem";
            }
        }

        /// <summary>
        /// response
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="num"></param>
        /// <returns></returns>
        string IDriverModel.FormatFrameResponse(byte[] frame, System.Globalization.NumberStyles num)
        {
            if (frame == null)
                return "Empty reponse";

            switch (num)
            {
                case NumberStyles.HexNumber:
                    return frame.Aggregate("", (a, e) => a + " " + e.ToString("X"));

                case NumberStyles.Integer:
                    return frame.Aggregate("", (a, e) => a + e.ToString());

                default:
                    return "Internal problem";
            }
        }

        /// <summary>
        /// Aktywnosc poloczenia
        /// </summary>
        bool IDriverModel.isAlive
        {
            get { return isLive; }
            set
            {
                isLive = value;
            }
        }

        /// <summary>
        /// Zajetosc sterownika
        /// </summary>
        bool IDriverModel.isBusy
        {
            get { return bWorker.IsBusy; }
        }

        /// <summary>
        /// Send data
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        byte[] IDriverModel.sendBytes(byte[] data)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Pluginy
        /// </summary>
        object[] IDriverModel.plugins
        {
            get { throw new NotImplementedException(); }
        }

        /// <summary>
        /// Rozne parametry
        /// 1- Wlazenie rozszerzonej adresacji dla Device
        /// 2- Wlaczenie Block Adress
        /// </summary>
        bool[] IDriverModel.AuxParam
        {
            get { return new Boolean[] { false, true }; }
        }

        private string getAreaFromFrame(byte[] ask)
        {
            return "cos";
        }

        //Do wyswietlania
        public override string ToString()
        {
            return "Driver: " + ((IDriverModel)this).driverName;
        }

        #region IDisposable Support

        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    lock (commLock)
                    {
                        if (dc.Connected)
                            dc.Disconnect();
                    }

                    bWorker.Dispose();
                }

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~Driver() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }

        #endregion IDisposable Support
    }
}