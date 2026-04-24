using ProjectDataLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace nmDriver
{
    public class Driver : IDriverModel, IDisposable
    {
        // Constants for access
        private const byte fctReadCoil = 1;

        private const byte fctReadDiscreteInputs = 2;
        private const byte fctReadHoldingRegister = 3;
        private const byte fctReadInputRegister = 4;
        private const byte fctWriteSingleCoil = 5;
        private const byte fctWriteSingleRegister = 6;
        private const byte fctWriteMultipleCoils = 15;
        private const byte fctWriteMultipleRegister = 16;

        //Głowny Buffor danych punkt wejsciowy dla obslugi tagów
        private List<Tag> tagList = new List<Tag>();

        //Beckgroundworker
        [NonSerialized]
        private
        //Watek wtle
        BackgroundWorker bWorker;

        [field: NonSerialized]
        private EventHandler sendInfoEv;

        [field: NonSerialized]
        private EventHandler errorSendEv;

        [field: NonSerialized]
        private EventHandler refreshedPartial;

        [field: NonSerialized]
        private EventHandler refreshCycleEv;

        [field: NonSerialized]
        private EventHandler sendLogInfoEv;

        [field: NonSerialized]
        private EventHandler reciveLogInfoEv;

        /// <summary>
        /// Obszary Pamieci dla sterownika
        /// </summary>
        private static string CoilsName = "Coils";

        private static string DigitalInputsName = "DigitalInputs";
        private static string InputRegisterName = "InputRegisters";
        private static string HoldingRegisterName = "HoldingRegisters";

        /// <summary>
        /// Zapytanie do urzadzen zdalnych
        /// </summary>
        private List<byte[]> mainFrames = new List<byte[]>();

        /// <summary>
        /// Klient poloczenia
        /// </summary>
        private TcpClient tcpClient = new TcpClient();

        /// <summary>
        /// Parametry Transmisji
        /// </summary>
        private TcpDriverParam DriverParam_;

        private Guid ObjId_;

        Guid IDriverModel.ObjId
        {
            get { return ObjId_; }
        }

        /// <summary>
        /// Nazwa Sterownika
        /// </summary>
        string IDriverModel.driverName
        {
            get { return "ModbusMasterTCP"; }
        }

        /// <summary>
        /// Start Cyklu
        /// </summary>
        /// <param name="tagsList"></param>
        /// <returns></returns>
        bool IDriverModel.activateCycle(List<ITag> tagsList1)
        {
            try
            {
                if (bWorker.IsBusy)
                    return false;

                //Przypisanie danych
                this.tagList.Clear();

                //Konwersja
                List<Tag> comTgs = (from c in tagsList1 where c is Tag select (Tag)c).ToList();

                //Reszta kodu
                this.tagList.AddRange(comTgs);

                //Konfiguracja danych
                ConfigData(this.tagList);

                //Nowy client
                tcpClient = new TcpClient();
                tcpClient.Connect(DriverParam_.Ip, DriverParam_.Port);

                //Worker
                bWorker.RunWorkerAsync();

                //Comm jest aktywna
                isLive = true;

                foreach (IDriverModel it in this.tagList)
                    it.isAlive = true;

                return true;
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
        bool IDriverModel.addTagsComm(List<ITag> tagList1)
        {
            try
            {
                //Konwersja
                List<Tag> comTgs = (from c in tagList1 where c is Tag select (Tag)c).ToList();

                //Dodanie Tagow
                this.tagList.AddRange(comTgs);

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
        /// Odjecie tagow
        /// </summary>
        /// <param name="tagList"></param>
        /// <returns></returns>
        bool IDriverModel.removeTagsComm(List<ITag> tagList2)
        {
            try
            {
                //Konwersja
                List<Tag> comTgs = (from c in tagList2 where c is Tag select (Tag)c).ToList();

                //usuniecie danych
                foreach (Tag tn in comTgs)
                    tagList.Remove(tn);

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
        /// Stop Cyklu
        /// </summary>
        /// <returns></returns>
        bool IDriverModel.deactivateCycle()
        {
            isLive = false;

            foreach (IDriverModel it in this.tagList)
                it.isAlive = false;

            return true;
        }

        /// <summary>
        /// Cykl odwierzono
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
        /// Informacja o obszarach pamięci
        /// </summary>
        MemoryAreaInfo[] IDriverModel.MemoryAreaInf
        {
            get
            {
                return new MemoryAreaInfo[] {
                    new MemoryAreaInfo(CoilsName,1,1),
                    new MemoryAreaInfo(DigitalInputsName,1,2),
                    new MemoryAreaInfo(InputRegisterName,16,4),
                    new MemoryAreaInfo(HoldingRegisterName,16,3) };
            }
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
        /// Informacja o działąniu komunikacji
        /// </summary>

        private Boolean isLive;

        bool IDriverModel.isAlive
        {
            get { return isLive; }
            set
            {
                isLive = value;
            }
        }

        /// <summary>
        /// Pluginy dostępne
        /// </summary>
        object[] IDriverModel.plugins
        {
            get { return null; }
        }

        /// <summary>
        /// Konstruktor
        /// </summary>
        public Driver()
        {
            try
            {
                //inicjacja bitu
                isLive = false;

                //Parametry Tramsmisji
                DriverParam_ = new TcpDriverParam();

                //Nowy Guid
                ObjId_ = Guid.NewGuid();

                //Obsluga Backgriundworker
                bWorker = new BackgroundWorker();
                bWorker.WorkerSupportsCancellation = true;
                bWorker.DoWork += new DoWorkEventHandler(bWorker_DoWork);
                bWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bWorker_RunWorkerCompleted);
            }
            catch (Exception Ex)
            {
                if (errorSendEv != null)
                    errorSendEv(this, new ProjectEventArgs(new byte[] { 0 }, DateTime.Now, "ModbusMasterTCP.Constructor :" + Ex.Message, Ex));
            }
        }

        /// <summary>
        /// Głowna praca sterownika - pętla obiegowa
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void bWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            //Sprawdz cy klient jest poloczony
            if (!tcpClient.Connected)
                return;

            if (mainFrames.Count > 0)
            {
                try
                {
                    //Zapytanie
                    foreach (byte[] fr in mainFrames)
                    {
                        makeTranscation(fr);
                        Thread.Sleep(10);
                    }
                }
                catch (Exception Ex)
                {
                    if (errorSendEv != null)
                        errorSendEv(this, new ProjectEventArgs(mainFrames, DateTime.Now, "ModbusMasterRTU.bWorker_DoWork :" + Ex.Message, Ex));
                }
            }

            #region Zapisz dane na porcie

            //Tagi z obszaru Coils do zapisu
            List<Tag> COWrite = tagList.Where(x => x.areaData.Equals(CoilsName) && x.setMod).ToList();
            if (COWrite.Count > 0)
            {
                try
                {
                    //Próba zapisu Taga
                    foreach (Tag tg in COWrite)
                    {
                        //Utworzenie do wyslania
                        BitArray btArray = new BitArray(tg.coreDataSend);
                        //Utworzenie tablicy byte
                        Byte[] buffByteArr = new byte[tg.coreDataSend.Length / 8 == 0 ? 1 : tg.coreDataSend.Length / 8];
                        //Konwersja i przeniesienie
                        btArray.CopyTo(buffByteArr, 0);

                        //Formowanie ramki
                        byte[] wrCoFrame = WriteMultipleCoils(tg.deviceAdress, tg.startData, tg.coreDataSend.Length, buffByteArr);
                        tg.setMod = false;

                        //Zdarzenia
                        if (sendLogInfoEv != null)
                            sendLogInfoEv(this, new ProjectEventArgs(wrCoFrame, DateTime.Now, getAreaFromFrame(wrCoFrame) + "REQUEST"));

                        //Wyslanie i oczekiwanie na zwrot Danych
                        byte[] rcvCo = writeDataOnPort(wrCoFrame);

                        //Sprawdzanie
                        if (rcvCo != null)
                        {
                            //Wyslanie zdarzenia o nadejsciu danych
                            if (reciveLogInfoEv != null)
                                reciveLogInfoEv(this, new ProjectEventArgs(rcvCo, DateTime.Now, getAreaFromFrame(rcvCo) + "RESPONSE"));

                            if (checkResponse(wrCoFrame, rcvCo))
                            {
                                //Pusto
                            }
                        }
                    }
                }
                catch (Exception Ex)
                {
                    if (errorSendEv != null)
                        errorSendEv(this, new ProjectEventArgs(new byte[] { 0 }, DateTime.Now, "ModbusMasterTCP.bWorker_DoWork.WriteCO :" + Ex.Message, Ex));
                }
            }

            //Tagi do zapisu w obszarze HoldingRegisters do zapisu
            List<Tag> HRWrite = tagList.Where(x => x.areaData.Equals(HoldingRegisterName) && x.setMod).ToList();
            if (HRWrite.Count > 0)
            {
                try
                {
                    foreach (Tag tg in HRWrite)
                    {
                        //Utworzenie do wyslania
                        BitArray btArray = new BitArray(tg.coreDataSend);
                        //Utworzenie tablicy byte
                        Byte[] buffByteArr = new byte[tg.coreDataSend.Length / 8];
                        //Konwersja i przeniesienie
                        btArray.CopyTo(buffByteArr, 0);

                        //Formowanie ramki
                        byte[] wrHRFrame = WriteMultipleRegister(tg.deviceAdress, tg.startData, buffByteArr.Length / 2, buffByteArr);
                        tg.setMod = false;

                        //Zdarzenia
                        if (sendLogInfoEv != null)
                            sendLogInfoEv(this, new ProjectEventArgs(wrHRFrame, DateTime.Now, getAreaFromFrame(wrHRFrame) + "REQUEST"));

                        //Wyslanie i oczekiwanie na zwrot Danych
                        byte[] rcvBuff = writeDataOnPort(wrHRFrame);

                        //Sprawdzenie
                        if (rcvBuff != null)
                        {
                            //Wyslanie zdarzenia o nadejsciu danych
                            if (reciveLogInfoEv != null)
                                reciveLogInfoEv(this, new ProjectEventArgs(rcvBuff, DateTime.Now, getAreaFromFrame(rcvBuff) + "RESPONSE"));

                            if (checkResponse(wrHRFrame, rcvBuff))
                            {
                                //Pusto
                            }
                        }
                    }
                }
                catch (Exception Ex)
                {
                    if (errorSendEv != null)
                        errorSendEv(this, new ProjectEventArgs(new byte[] { 0 }, DateTime.Now, "ModbusMasterTCP.bWorker_DoWork.WriteHR :" + Ex.Message, Ex));
                }
            }

            #endregion Zapisz dane na porcie

            //Opoznienie wątku o dany czas
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
                //Reaktywacja pobierania danych
                if (isLive)
                {
                    bWorker.RunWorkerAsync();
                }
                else
                {
                    tcpClient.GetStream().Close();
                    tcpClient.Close();
                }
            }
            catch (Exception Ex)
            {
                if (errorSendEv != null)
                    errorSendEv(this, new ProjectEventArgs(new byte[] { 0 }, DateTime.Now, "ModbusMasterTCP.bWorker_RunWorkerCompleted :" + Ex.Message, Ex));
            }
        }

        /// <summary>
        /// Wykonuje Transakcje z urządzniem zdalnym
        /// </summary>
        /// <param name="frRequest"></param>
        /// <returns></returns>
        private Boolean makeTranscation(byte[] frRequest)
        {
            //Zdarzenia
            if (sendLogInfoEv != null)
                sendLogInfoEv(this, new ProjectEventArgs(frRequest, DateTime.Now, getAreaFromFrame(frRequest) + "REQUEST"));

            //Fukcja zwraca dane odpowiedzi jezeli pakiet jest pusty jest Timeout
            byte[] frResponse = writeDataOnPort(frRequest);

            //Sprawdzanie danych
            if (frResponse != null)
            {
                //Wyslanie zdarzenia o nadejsciu danych
                if (reciveLogInfoEv != null)
                    reciveLogInfoEv(this, new ProjectEventArgs(frResponse, DateTime.Now, getAreaFromFrame(frResponse) + "RESPONSE"));

                //Sprawdzenie odpowiedzi
                if (checkResponse(frRequest, frResponse))
                {
                    //Paramery wejsciowe
                    int startIndex = BitConverter.ToUInt16(new byte[] { frRequest[9], frRequest[8] }, 0);
                    int countData = BitConverter.ToUInt16(new byte[] { frRequest[11], frRequest[10] }, 0);
                    int devAdr = (int)frResponse[6];

                    //Zamiana memoryArea ze struktury byte[] na Boolean[]. Odfiltrowanie danych
                    BitArray btArr = new BitArray(getDataFrame(frResponse, countData).ToArray());
                    Boolean[] bitArray = new Boolean[btArr.Length];
                    //Przekopiowanie bitow do tablicy pomocniczej
                    btArr.CopyTo(bitArray, 0);

                    //Obszar pamiecie
                    MemoryAreaInfo mInfo = ((IDriverModel)this).MemoryAreaInf.Where(x => x.fctCode == frResponse[7]).ToArray()[0];

                    //Selekcja tagow do przeslania do odswierzenia
                    var zm = from n in tagList
                             where n.deviceAdress == devAdr
                             && n.areaData == mInfo.Name
                             && n.bitAdres >= startIndex * mInfo.AdresSize //Dolny zakres
                             && n.bitAdres + n.coreData.Length <= (startIndex + countData) * mInfo.AdresSize //Gorny zakres
                             select n;

                    //Lista
                    List<Tag> refDataTag = zm.ToList();

                    //Przypisane danych do tagow
                    for (int i = 0; i < refDataTag.Count; i++)
                    {
                        Array.Copy(bitArray, refDataTag[i].bitAdres - startIndex * mInfo.AdresSize, refDataTag[i].coreData, 0, refDataTag[i].coreData.Length);
                        refDataTag[i].refreshData();
                    }

                    //Odswierzenie danych informacja. Delegat do pozostałych klass
                    if (refreshCycleEv != null)
                        refreshCycleEv(this, new ProjectEventArgs(refDataTag));

                    return true;
                }
            }
            else
            {
                //OverTime
                if (sendInfoEv != null)
                    sendInfoEv(this, new ProjectEventArgs(frRequest, DateTime.Now, "Device not respond. " + getAreaFromFrame(frRequest) + "- Timeout"));

                return false;
            }

            return true;
        }

        /// <summary>
        /// Zapisz dane na porcie
        /// </summary>
        /// <param name="dane"></param>
        /// <returns></returns>
        private byte[] writeDataOnPort(byte[] dane)
        {
            //Zapis na port
            tcpClient.GetStream().Write(dane, 0, dane.Length);

            //Czekanie na odpowiedz
            for (int i = 0; i < DriverParam_.Timeout; i = i + 5)
            {
                //Nadeszły dane
                if (tcpClient.GetStream().DataAvailable)
                {
                    //Buffor na dane
                    byte[] Response = new byte[tcpClient.Available];
                    tcpClient.GetStream().Read(Response, 0, Response.Length);
                    //Oczyszczenie strumienia
                    tcpClient.GetStream().Flush();
                    return Response;
                }

                //Opoznienie watku
                Thread.Sleep(5);
            }

            //Jezeli program dojedzie do tego miejsca mamy overtime
            return null;
        }

        /// <summary>
        /// Pobranie dane z ramki odpowiedzi
        /// </summary>
        /// <param name="dane"></param>
        /// <returns></returns>
        private List<byte> getDataFrame(byte[] dane, int count)
        {
            try
            {
                //Pobranie nagłowka
                Byte[] header = dane.ToList().GetRange(0, 9).ToArray();

                //Wyslanie ramki
                return dane.ToList().GetRange(9, header[8]);
            }
            catch (Exception Ex)
            {
                if (errorSendEv != null)
                    errorSendEv(this, new ProjectEventArgs(new byte[] { 0 }, DateTime.Now, "ModbusMasterTCP.getDataFrame :" + Ex.Message, Ex));

                return null;
            }
        }

        /// <summary>
        /// Konfiguracja parmamertów Bufforów dla rejestrow
        /// </summary>
        /// <param name="tagList"></param>
        private void ConfigData(List<Tag> tagList)
        {
            //Testowanie strownika
            if (tagList == null)
                return;

            //Tablica zapytań
            List<byte[]> buffFrames = new List<byte[]>();

            //Pobranie adresów
            List<ushort> adreses = tagList.Select(x => x.deviceAdress).ToList();

            //Pobranie unikalnych adresow
            adreses = adreses.Distinct().ToList();

            //Tworzenie Ramek
            foreach (int i in adreses)
                buffFrames.AddRange(configSingleDevice(tagList.Where(x => x.deviceAdress == i).ToList(), i));

            //Przypisanie zapytań
            mainFrames = buffFrames;
        }

        /// <summary>
        /// Program zakresy odpytan dla jednego adresu
        /// </summary>
        /// <param name="tgs"></param>
        /// <returns></returns>
        private List<byte[]> configSingleDevice(List<Tag> tgs, int adress)
        {
            int maxFrameSizeCO = 230 * 8;
            int maxSizeCO = 130;

            int maxFrameSizeDI = 230 * 8;
            int maxSizeDI = 130;

            int maxFrameSizeHR = 110;
            int maxSizeHR = 20;

            int maxFrameSizeIR = 110;
            int maxSizeIR = 20;

            //Tablica pomocnicza
            List<byte[]> buffFrames = new List<byte[]>();

            try
            {
                //Znalezienie obszarów max i min do odczytu dla całego odwzorowania rejestrów
                //Szakuanie granic odswierzania COILS
                List<Tag> coilsTag = new List<Tag>();
                coilsTag = tgs.Where(x => x.areaData.Equals(CoilsName)).ToList<Tag>();
                if (coilsTag.Count > 0)
                {
                    //Najnizszy i najwyzszy adres tagowy.
                    int min = coilsTag.Min(x => x.startData);
                    int max = coilsTag.Max(x => (x.startData + x.coreData.Length)) - 1;
                    int marker = 0;

                    //Lista punktow do utworzenia ramek
                    List<PO> points = new List<PO>();
                    points.Add(new PO(min, 0));

                    //Petla obiegowa
                    for (int i = min; i <= max; i++)
                    {
                        #region Zabezpiecznie dlugiej ramki

                        if (points[points.Count - 1].Y - points[points.Count - 1].X >= maxFrameSizeCO)
                        {
                            //Jesteśmy w miejscu Taga trzeba podac koniec
                            Tag halfTag = coilsTag.Where(x => x.startData <= i && (x.startData + x.coreData.Length) > i).ToList()[0];
                            if (halfTag != null)
                            {
                                points[points.Count - 1].Y = halfTag.startData + halfTag.coreData.Length - 1;
                                i = points[points.Count - 1].Y + 1;
                                points.Add(new PO(i, 0));
                            }
                            else
                                points.Add(new PO(i, 0));
                        }

                        #endregion Zabezpiecznie dlugiej ramki

                        #region Zabezpieczenie przed przekroczeniem wolnego odstepu

                        if (marker == maxSizeCO)
                            points.Add(new PO(i, 0));

                        #endregion Zabezpieczenie przed przekroczeniem wolnego odstepu

                        #region Sprawdzenie czy tagi sa w zakresie

                        if (coilsTag.Where(x => x.startData <= i && (x.startData + x.coreData.Length) > i).ToList().Count > 0)
                        {
                            //DANIE
                            points[points.Count - 1].Y = i;

                            //Dociagnicie do adresu po powrocie do odliczania
                            if (marker >= maxSizeCO)
                                points[points.Count - 1].X = i;

                            marker = 0;
                        }
                        else
                        {
                            //Przesuwaj X
                            if (marker >= maxSizeCO)
                                points[points.Count - 1].X = i;

                            //Inkrementuj marker
                            marker++;
                        }

                        #endregion Sprawdzenie czy tagi sa w zakresie
                    }

                    //Utwzorzenie ramek
                    foreach (PO p in points)
                    {
                        buffFrames.Add(CreateReadHeader((byte)adress, (ushort)p.X, (ushort)((p.Y - p.X) + 1), fctReadCoil));
                    }
                }
            }
            catch (Exception Ex)
            {
                if (errorSendEv != null)
                    errorSendEv(this, new ProjectEventArgs(new byte[] { 0 }, DateTime.Now, "ModbusMasterRTU.configSingleDevice.CO :" + Ex.Message, Ex));
            }

            try
            {
                //Znalezienie obszarów max i min do odczytu dla całego odwzorowania rejestrów
                //Szakuanie granic odswierzania DISCRETEINPUT
                List<Tag> DiscreteInputTag = new List<Tag>();
                DiscreteInputTag = tgs.Where(x => x.areaData.Equals(DigitalInputsName)).ToList<Tag>();
                if (DiscreteInputTag.Count > 0)
                {
                    //Najnizszy i najwyzszy adres tagowy.
                    int min = DiscreteInputTag.Min(x => x.startData);
                    int max = DiscreteInputTag.Max(x => (x.startData + x.coreData.Length)) - 1;
                    int marker = 0;

                    //Lista punktow do utworzenia ramek
                    List<PO> points = new List<PO>();
                    points.Add(new PO(min, 0));

                    //Petla obiegowa
                    for (int i = min; i <= max; i++)
                    {
                        //Zabezpiecznie dlugiej ramki
                        if (points[points.Count - 1].Y - points[points.Count - 1].X >= maxFrameSizeDI)
                        {
                            //Jestemy w miejscu Taga trzeba podac koniec
                            Tag halfTag = DiscreteInputTag.Where(x => x.startData <= i && (x.startData + x.coreData.Length) > i).ToList()[0];
                            if (halfTag != null)
                            {
                                points[points.Count - 1].Y = halfTag.startData + halfTag.coreData.Length - 1;
                                i = points[points.Count - 1].Y + 1;
                                points.Add(new PO(i, 0));
                            }
                            else
                                points.Add(new PO(i, 0));
                        }

                        //Zabezpieczenie przed przekroczeniem wolnego odstepu
                        if (marker == maxSizeDI)
                            points.Add(new PO(i, 0));

                        //Sprawdzenie czy tagi sa w zakresie
                        if (DiscreteInputTag.Where(x => x.startData <= i && (x.startData + x.coreData.Length) > i).ToList().Count > 0)
                        {
                            points[points.Count - 1].Y = i;

                            //Dociagnicie do adresu po powrocie do odliczania
                            if (marker >= maxSizeDI)
                                points[points.Count - 1].X = i;

                            marker = 0;
                        }
                        else
                        {
                            //Przesuwaj X
                            if (marker >= maxSizeDI)
                                points[points.Count - 1].X = i;

                            //Inkrementuj marker
                            marker++;
                        }
                    }

                    //Utwzorzenie ramek
                    foreach (PO p in points)
                    {
                        buffFrames.Add(CreateReadHeader((byte)adress, (ushort)p.X, (ushort)((p.Y - p.X) + 1), fctReadDiscreteInputs));
                    }
                }
            }
            catch (Exception Ex)
            {
                if (errorSendEv != null)
                    errorSendEv(this, new ProjectEventArgs(new byte[] { 0 }, DateTime.Now, "ModbusMasterRTU.configSingleDevice.DI :" + Ex.Message, Ex));
            }

            try
            {
                //Znalezienie obszarów max i min do odczytu dla całego odwzorowania rejestrów
                List<Tag> InputRegisterTag = new List<Tag>();
                InputRegisterTag = tgs.Where(x => x.areaData.Equals(InputRegisterName)).ToList<Tag>();
                if (InputRegisterTag.Count > 0)
                {
                    //Najnizszy i najwyzszy adres tagowy.
                    int min = InputRegisterTag.Min(x => x.startData);
                    int max = InputRegisterTag.Max(x => (x.startData + x.coreData.Length / 16)) - 1;
                    int marker = 0;

                    //Lista punktow do utworzenia ramek
                    List<PO> points = new List<PO>();
                    points.Add(new PO(min, 0));

                    //Petla obiegowa
                    for (int i = min; i <= max; i++)
                    {
                        //Zabezpiecznie dlugiej ramki
                        if (points[points.Count - 1].Y - points[points.Count - 1].X >= maxFrameSizeIR)
                        {
                            Tag halfTag = InputRegisterTag.Where(x => x.startData <= i && (x.startData + x.coreData.Length / 16) > i).ToList()[0];
                            if (halfTag != null)
                            {
                                points[points.Count - 1].Y = halfTag.startData + (halfTag.coreData.Length / 16) - 1;
                                i = points[points.Count - 1].Y + 1;
                                points.Add(new PO(i, 0));
                            }
                            else
                                points.Add(new PO(i, 0));
                        }

                        //Zabezpieczenie przed przekroczeniem wolnego odstepu
                        if (marker == maxSizeIR)
                            points.Add(new PO(i, 0));

                        //Sprawdzenie czy tagi sa w zakresie
                        if (InputRegisterTag.Where(x => x.startData <= i && (x.startData + x.coreData.Length / 16) > i).ToList().Count > 0)
                        {
                            points[points.Count - 1].Y = i;

                            //Dociagnicie do adresu po powrocie do odliczania
                            if (marker >= maxSizeIR)
                                points[points.Count - 1].X = i;

                            marker = 0;
                        }
                        else
                        {
                            //Przesuwaj X
                            if (marker >= maxSizeIR)
                                points[points.Count - 1].X = i;

                            //Inkrementuj marker
                            marker++;
                        }
                    }

                    //Utwzorzenie ramek
                    foreach (PO p in points)
                    {
                        buffFrames.Add(CreateReadHeader((byte)adress, (ushort)p.X, (ushort)((p.Y - p.X) + 1), fctReadInputRegister));
                    }
                }
            }
            catch (Exception Ex)
            {
                if (errorSendEv != null)
                    errorSendEv(this, new ProjectEventArgs(new byte[] { 0 }, DateTime.Now, "ModbusMasterRTU.configSingleDevice.IR :" + Ex.Message, Ex));
            }

            try
            {
                //Znalezienie obszarów max i min do odczytu dla całego odwzorowania rejestrów
                List<Tag> HoldingRegisterTag = new List<Tag>();
                HoldingRegisterTag = tgs.Where(x => x.areaData.Equals(HoldingRegisterName)).ToList<Tag>();
                if (HoldingRegisterTag.Count > 0)
                {
                    //Najnizszy i najwyzszy adres tagowy.
                    int min = HoldingRegisterTag.Min(x => x.startData);
                    int max = HoldingRegisterTag.Max(x => (x.startData + x.coreData.Length / 16)) - 1;
                    int marker = 0;

                    //Lista punktow do utworzenia ramek
                    List<PO> points = new List<PO>();
                    points.Add(new PO(min, 0));

                    //Petla obiegowa
                    for (int i = min; i <= max; i++)
                    {
                        //Zabezpiecznie dlugiej ramki
                        if (points[points.Count - 1].Y - points[points.Count - 1].X >= maxFrameSizeHR)
                        {
                            //Jestemy w miejscu Taga trzeba podac koniec
                            Tag halfTag = HoldingRegisterTag.Where(x => x.startData <= i && (x.startData + x.coreData.Length / 16) > i).ToList()[0];
                            if (halfTag != null)
                            {
                                points[points.Count - 1].Y = halfTag.startData + (halfTag.coreData.Length / 16) - 1;
                                i = points[points.Count - 1].Y + 1;
                                points.Add(new PO(i, 0));
                            }
                            else
                                points.Add(new PO(i, 0));
                        }

                        //Zabezpieczenie przed przekroczeniem wolnego odstepu
                        if (marker == maxSizeHR)
                            points.Add(new PO(i, 0));

                        //Sprawdzenie czy tagi sa w zakresie
                        if (HoldingRegisterTag.Where(x => x.startData <= i && (x.startData + x.coreData.Length / 16) > i).ToList().Count > 0)
                        {
                            points[points.Count - 1].Y = i;

                            //Dociagnicie do adresu po powrocie do odliczania
                            if (marker >= maxSizeHR)
                                points[points.Count - 1].X = i;

                            marker = 0;
                        }
                        else
                        {
                            //Przesuwaj X
                            if (marker >= maxSizeHR)
                                points[points.Count - 1].X = i;

                            //Inkrementuj marker
                            marker++;
                        }
                    }

                    //Utwzorzenie ramek
                    foreach (PO p in points)
                    {
                        buffFrames.Add(CreateReadHeader((byte)adress, (ushort)p.X, (ushort)((p.Y - p.X) + 1), fctReadHoldingRegister));
                    }
                }
            }
            catch (Exception Ex)
            {
                if (errorSendEv != null)
                    errorSendEv(this, new ProjectEventArgs(new byte[] { 0 }, DateTime.Now, "ModbusMasterRTU.configSingleDevice.HR :" + Ex.Message, Ex));
            }

            return buffFrames;
        }

        // ------------------------------------------------------------------------
        /// <summary>Write single coil in slave asynchronous. The result is given in the response function.</summary>
        /// <param name="id">Unique id that marks the transaction. In asynchonous mode this id is given to the callback function.</param>
        /// <param name="startAddress">Address from where the data read begins.</param>
        /// <param name="OnOff">Specifys if the coil should be switched on or off.</param>
        private byte[] WriteSingleCoils(int id, int startAddress, bool OnOff)
        {
            byte[] data;
            data = CreateWriteHeader(id, startAddress, 1, 1, fctWriteSingleCoil);
            if (OnOff == true) data[10] = 255;
            else data[10] = 0;
            return data;
        }

        // ------------------------------------------------------------------------
        /// <summary>Write multiple coils in slave asynchronous. The result is given in the response function.</summary>
        /// <param name="id">Unique id that marks the transaction. In asynchonous mode this id is given to the callback function.</param>
        /// <param name="startAddress">Address from where the data read begins.</param>
        /// <param name="numBits">Specifys number of bits.</param>
        /// <param name="values">Contains the bit information in byte format.</param>
        private byte[] WriteMultipleCoils(int id, int startAddress, int numBits, byte[] values)
        {
            byte numBytes = Convert.ToByte(values.Length);
            byte[] data;
            data = CreateWriteHeader(id, startAddress, numBits, (byte)(numBytes + 2), fctWriteMultipleCoils);
            Array.Copy(values, 0, data, 13, numBytes);
            return data;
        }

        // ------------------------------------------------------------------------
        /// <summary>Write single register in slave asynchronous. The result is given in the response function.</summary>
        /// <param name="id">Unique id that marks the transaction. In asynchonous mode this id is given to the callback function.</param>
        /// <param name="startAddress">Address from where the data read begins.</param>
        /// <param name="values">Contains the register information.</param>
        private byte[] WriteSingleRegister(int id, int startAddress, byte[] values)
        {
            byte[] data;
            data = CreateWriteHeader(id, startAddress, 1, 1, fctWriteSingleRegister);
            data[10] = values[0];
            data[11] = values[1];
            return data;
        }

        // ------------------------------------------------------------------------
        /// <summary>Write multiple registers in slave asynchronous. The result is given in the response function.</summary>
        /// <param name="id">Unique id that marks the transaction. In asynchonous mode this id is given to the callback function.</param>
        /// <param name="startAddress">Address from where the data read begins.</param>
        /// <param name="numRegs">Number of registers to be written.</param>
        /// <param name="values">Contains the register information.</param>
        private byte[] WriteMultipleRegister(int id, int startAddress, int numRegs, byte[] values)
        {
            byte numBytes = Convert.ToByte(values.Length);
            byte[] data;
            data = CreateWriteHeader(id, startAddress, numRegs, (byte)(numBytes + 2), fctWriteMultipleRegister);
            Array.Copy(values, 0, data, 13, numBytes);
            return data;
        }

        // ------------------------------------------------------------------------
        // Create modbus header for read action
        private byte[] CreateReadHeader(int id, int startAddress, ushort length, byte function)
        {
            byte[] data = new byte[12];

            byte[] _id = BitConverter.GetBytes((ushort)id);
            data[0] = _id[1];				// Slave id high byte
            data[1] = _id[0];				// Slave id low byte
            data[5] = 6;					// Message size
            data[6] = _id[0];				// Slave address
            data[7] = function;				// Function code

            byte[] _adr = BitConverter.GetBytes((ushort)startAddress);
            data[8] = _adr[1];				// Start address
            data[9] = _adr[0];				// Start address

            //ilość danych
            byte[] length_ = BitConverter.GetBytes(length);
            data[10] = length_[1];
            data[11] = length_[0];

            return data;
        }

        // ------------------------------------------------------------------------
        // Create modbus header for write action
        private byte[] CreateWriteHeader(int id, int startAddress, int numData, byte numBytes, byte function)
        {
            byte[] data = new byte[numBytes + 11];

            byte[] _id = BitConverter.GetBytes((short)id);
            data[0] = _id[0];				// Slave id high byte
            data[1] = _id[1];				// Slave id low byte+
            byte[] _size = BitConverter.GetBytes((short)IPAddress.HostToNetworkOrder((short)(5 + numBytes)));
            data[4] = _size[0];				// Complete message size in bytes
            data[5] = _size[1];				// Complete message size in bytes
            data[6] = _id[0];				// Slave address
            data[7] = function;				// Function code
            byte[] _adr = BitConverter.GetBytes((short)IPAddress.HostToNetworkOrder((short)startAddress));
            data[8] = _adr[0];				// Start address
            data[9] = _adr[1];				// Start address
            if (function >= fctWriteMultipleCoils)
            {
                byte[] cnt = BitConverter.GetBytes((short)IPAddress.HostToNetworkOrder((short)numData));
                data[10] = cnt[0];			// Number of bytes
                data[11] = cnt[1];			// Number of bytes
                data[12] = (byte)(numBytes - 2);
            }

            return data;
        }

        /// <summary>
        /// Sprawdzenie Ramek z odpowiedzi
        /// </summary>
        /// <param name="request"></param>
        /// <param name="response"></param>
        /// <returns></returns>
        private Boolean checkResponse(byte[] request, byte[] response)
        {
            try
            {
                //Bledne ramki
                if (request.Equals(null) || response.Equals(null) || response.Length < 9)
                {
                    //Informacja o bledzie
                    if (sendInfoEv != null)
                        sendInfoEv(this, new ProjectEventArgs(response, DateTime.Now, "Bad size frame from remote device!"));

                    return false;
                }

                //Gdy ramka jest normalna. Nie zawiera informacji o wyjątku
                if (!response.Length.Equals(9))
                {
                    //Sprawdzenie części TCP Protocols ID, Transaction ID
                    if (!request[0].Equals(response[0]) || !request[1].Equals(response[1]) || !request[2].Equals(response[2]) || !request[3].Equals(response[3]))
                    {
                        //Informacja o bledzie
                        if (sendInfoEv != null)
                            sendInfoEv(this, new ProjectEventArgs(request, DateTime.Now, "Transaction ID or Protocols ID isnt correct!"));

                        return false;
                    }

                    //Sprawdzenie dlugości ramki podanej w nagłówku TCP. Czy odpowida faktycznej ilości danych
                    int lengthTCP = BitConverter.ToInt16(new byte[2] { response[5], response[4] }, 0);
                    if (!lengthTCP.Equals(response.Length - 6))
                    {
                        //Informacja o bledzie
                        if (sendInfoEv != null)
                            sendInfoEv(this, new ProjectEventArgs(request, DateTime.Now, "Lenght in TCP Header isnt correct!"));

                        return false;
                    }

                    //Sprawdzenie kodu zwracanej fukcji i adresu urzadzenia
                    if (!request[6].Equals(response[6]) || !request[7].Equals(response[7]))
                    {
                        if (sendInfoEv != null)
                            sendInfoEv(this, new ProjectEventArgs(request, DateTime.Now, "Device ID or Function Code isnt correct!"));

                        return false;
                    }

                    //Bloki odpowidające fukcją Read DI, CO, HR, IR
                    if (request[7].Equals(1) || request[7].Equals(2) || request[7].Equals(3) || request[7].Equals(4))
                    {
                        //Weryfikacja dlugości odpowidz
                        if (response[8] != response.Length - 9)
                        {
                            if (sendInfoEv != null)
                                sendInfoEv(this, new ProjectEventArgs(request, DateTime.Now, "Length request frame isnt correct! Too many Tags to read"));

                            return false;
                        }

                        //Sprawdzenie czy nadeszła ilosć danych która powinna nadejść
                        int countReq = BitConverter.ToInt16(new byte[2] { request[11], request[10] }, 0);
                        if (request[7].Equals(1) || request[7].Equals(2))
                        {
                            //DI CO
                            int reszta = (int)((float)countReq % 8);
                            int byteCount = countReq / 8;

                            if (reszta > 0)
                                byteCount++;

                            //Sprawdzanie odpowiedzi
                            if (byteCount != response[8])
                            {
                                if (sendInfoEv != null)
                                    sendInfoEv(this, new ProjectEventArgs(request, DateTime.Now, "Response length isnt appropriate!"));

                                return false;
                            }

                            return true;
                        }
                        else
                        {
                            //HR  IR
                            if (countReq * 2 != response[8])
                            {
                                if (sendInfoEv != null)
                                    sendInfoEv(this, new ProjectEventArgs(request, DateTime.Now, "Response length isnt appropriate!"));

                                return false;
                            }

                            return true;
                        }
                    }
                }
                //Urzadzenie zwrociło wyjątek
                else
                {
                    if (sendInfoEv != null)
                        sendInfoEv(this, new ProjectEventArgs(request, DateTime.Now, String.Format("Exception: Function - {0}, Exception Nr. - {1}", response[7], response[8])));
                    return false;
                }

                return true;
            }
            catch (Exception Ex)
            {
                if (sendInfoEv != null)
                    sendInfoEv(this, new ProjectEventArgs(request, DateTime.Now, "ModbusMasterTCP.checkResponse :" + Ex.Message));

                return false;
            }
        }

        /// <summary>
        /// Pobiera nazwe obszaru pamieci na podstawie ramki
        /// </summary>
        /// <param name="ask"></param>
        /// <returns></returns>
        private string getAreaFromFrame(byte[] ask)
        {
            switch (ask[7])
            {
                case 1:
                    return CoilsName.ToUpper() + ".READ.";

                case 2:
                    return DigitalInputsName.ToUpper() + ".READ.";

                case 3:
                    return HoldingRegisterName.ToUpper() + ".READ.";

                case 4:
                    return InputRegisterName.ToUpper() + ".READ.";

                case 5:
                    return CoilsName.ToUpper() + ".WRITE.";

                case 6:
                    return HoldingRegisterName.ToUpper() + ".WRITE.";

                case 15:
                    return CoilsName.ToUpper() + ".WRITE.";

                case 16:
                    return HoldingRegisterName.ToUpper() + ".WRITE.";

                case 129:
                    return "Exception Nr: 129 (HEX: 81) ";

                case 130:
                    return "Exception Nr: 130 (HEX: 82) ";

                case 131:
                    return "Exception Nr: 131 (HEX: 83) ";

                case 132:
                    return "Exception Nr: 132 (HEX: 84) ";

                case 133:
                    return "Exception Nr: 133 (HEX: 85) ";

                case 134:
                    return "Exception Nr: 134 (HEX: 86) ";

                case 143:
                    return "Exception Nr: 143 (HEX: 8F) ";

                case 144:
                    return "Exception Nr: 144 (HEX: 90) ";

                default:
                    return ".NO NAME.";
            }
        }

        object IDriverModel.setDriverParam
        {
            get
            {
                return DriverParam_;
            }
            set
            {
                DriverParam_ = (TcpDriverParam)value;
            }
        }

        /// <summary>
        /// Formatowanie ramki w zalezności od odpowiedzi
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="num"></param>
        /// <returns></returns>
        string IDriverModel.FormatFrameRequest(byte[] frame, NumberStyles num)
        {
            try
            {
                String TcpHeader = String.Empty;
                String modbusHeader = String.Empty;
                String restData = string.Empty;

                switch (num)
                {
                    case NumberStyles.HexNumber:
                        if (frame[7] <= 4)
                        {
                            TcpHeader = String.Format("{0:X}{1:X}   {2:X}{3:X}   {4:X}{5:X}", frame[0], frame[1], frame[2], frame[3], frame[4], frame[5]);
                            modbusHeader = String.Format("{0:X}   {1:X}   {2:X}{3:X}   {4:X}{5:X}", frame[6], frame[7], frame[8], frame[9], frame[10], frame[11]);
                            return TcpHeader + "   " + modbusHeader;
                        }
                        else if (frame[7] == 5 || frame[7] == 6)
                        {
                            TcpHeader = String.Format("{0:X}{1:X}   {2:X}{3:X}   {4:X}{5:X}", frame[0], frame[1], frame[2], frame[3], frame[4], frame[5]);
                            modbusHeader = String.Format("{0:X}   {1:X}   {2:X}{3:X}   {4:X} {5:X}", frame[6], frame[7], frame[8], frame[9], frame[10], frame[11]);
                            return TcpHeader + "   " + modbusHeader;
                        }
                        else
                        {
                            TcpHeader = String.Format("{0:X}{1:X}   {2:X}{3:X}   {4:X}{5:X}", frame[0], frame[1], frame[2], frame[3], frame[4], frame[5]);
                            modbusHeader = String.Format("{0:X}   {1:X}   {2:X}{3:X}   {4:X}{5:X}   {6:X}", frame[6], frame[7], frame[8], frame[9], frame[10], frame[11], frame[12]);
                            restData = frame.ToList().GetRange(14, frame.Length - 14).Aggregate(String.Format("{0:X}", frame[13]), (sum, next) => sum + " " + String.Format("{0:X}", next));
                            return TcpHeader + "   " + modbusHeader + "   " + restData;
                        }

                    case NumberStyles.Integer:
                        if (frame[7] <= 4)
                        {
                            TcpHeader = String.Format("{0}{1}   {2}{3}   {4}{5}", frame[0], frame[1], frame[2], frame[3], frame[4], frame[5]);
                            modbusHeader = String.Format("{0}   {1}   {2}{3}   {4}{5}", frame[6], frame[7], frame[8], frame[9], frame[10], frame[11]);
                            return TcpHeader + "   " + modbusHeader;
                        }

                        if (frame[7] == 5 || frame[7] == 6)
                        {
                            TcpHeader = String.Format("{0}{1}   {2}{3}   {4}{5}", frame[0], frame[1], frame[2], frame[3], frame[4], frame[5]);
                            modbusHeader = String.Format("{0}   {1}   {2}{3}   {4} {5}", frame[6], frame[7], frame[8], frame[9], frame[10], frame[11]);
                            return TcpHeader + "   " + modbusHeader;
                        }
                        else
                        {
                            TcpHeader = String.Format("{0}{1}   {2}{3}   {4}{5}", frame[0], frame[1], frame[2], frame[3], frame[4], frame[5]);
                            modbusHeader = String.Format("{0}   {1}   {2}{3}   {4}{5}   {6}", frame[6], frame[7], frame[8], frame[9], frame[10], frame[11], frame[12]);
                            restData = frame.ToList().GetRange(14, frame.Length - 14).Aggregate(String.Format("{0}", frame[13]), (sum, next) => sum + " " + String.Format("{0}", next));
                            return TcpHeader + "   " + modbusHeader + "   " + restData;
                        }

                    default:
                        if (frame[7] <= 4)
                        {
                            TcpHeader = String.Format("{0}{1}   {2}{3}   {4}{5}", frame[0], frame[1], frame[2], frame[3], frame[4], frame[5]);
                            modbusHeader = String.Format("{0}   {1}   {2}{3}   {4}{5}", frame[6], frame[7], frame[8], frame[9], frame[10], frame[11]);
                            return TcpHeader + "   " + modbusHeader;
                        }

                        if (frame[7] == 5 || frame[7] == 6)
                        {
                            TcpHeader = String.Format("{0}{1}   {2}{3}   {4}{5}", frame[0], frame[1], frame[2], frame[3], frame[4], frame[5]);
                            modbusHeader = String.Format("{0}   {1}   {2}{3}   {4} {5}", frame[6], frame[7], frame[8], frame[9], frame[10], frame[11]);
                            return TcpHeader + "   " + modbusHeader;
                        }
                        else
                        {
                            TcpHeader = String.Format("{0}{1}   {2}{3}   {4}{5}", frame[0], frame[1], frame[2], frame[3], frame[4], frame[5]);
                            modbusHeader = String.Format("{0}   {1}   {2}{3}   {4}{5}   {6}", frame[6], frame[7], frame[8], frame[9], frame[10], frame[11], frame[12]);
                            restData = frame.ToList().GetRange(14, frame.Length - 14).Aggregate(String.Format("{0}", frame[13]), (sum, next) => sum + " " + String.Format("{0}", next));
                            return TcpHeader + "   " + modbusHeader + "   " + restData;
                        }
                }
            }
            catch (Exception Ex)
            {
                if (sendInfoEv != null)
                    sendInfoEv(this, new ProjectEventArgs(frame, DateTime.Now, "ModbusMasterTCP.FormatFrameRequest :" + Ex.Message));

                return "Conversion Foult";
            }
        }

        /// <summary>
        /// Formatowanie ramki przychodzącej
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="num"></param>
        /// <returns></returns>
        string IDriverModel.FormatFrameResponse(byte[] frame, NumberStyles num)
        {
            try
            {
                String TcpHeader = String.Empty;
                String modbusHeader = String.Empty;
                String restData = String.Empty;

                switch (num)
                {
                    case NumberStyles.HexNumber:
                        //Read Area
                        if (frame[7] <= 4)
                        {
                            TcpHeader = String.Format("{0:X}{1:X}   {2:X}{3:X}   {4:X}{5:X}", frame[0], frame[1], frame[2], frame[3], frame[4], frame[5]);
                            modbusHeader = String.Format("{0:X}   {1:X}   {2:X}", frame[6], frame[7], frame[8]);
                            restData = frame.ToList().GetRange(10, frame.Length - 10).Aggregate(String.Format("{0:X}", frame[9]), (sum, next) => sum + " " + String.Format("{0:X}", next));
                            return TcpHeader + "   " + modbusHeader + "   " + restData;
                        }
                        //Single Cols and Single Register
                        else if (frame[7] == 5 || frame[7] == 6)
                        {
                            TcpHeader = String.Format("{0:X}{1:X}   {2:X}{3:X}   {4:X}{5:X}", frame[0], frame[1], frame[2], frame[3], frame[4], frame[5]);
                            modbusHeader = String.Format("{0:X}   {1:X}   {2:X}{3:X}   {4:X} {5:X}", frame[6], frame[7], frame[8], frame[9], frame[10], frame[11]);
                            return TcpHeader + "   " + modbusHeader;
                        }
                        //Multiple Coils and Multiple Registers
                        else if (frame[7] == 15 || frame[7] == 16)
                        {
                            TcpHeader = String.Format("{0:X}{1:X}   {2:X}{3:X}   {4:X}{5:X}", frame[0], frame[1], frame[2], frame[3], frame[4], frame[5]);
                            modbusHeader = String.Format("{0:X}   {1:X}   {2:X}{3:X}   {4:X}{5:X}", frame[6], frame[7], frame[8], frame[9], frame[10], frame[11]);
                            return TcpHeader + "   " + modbusHeader;
                        }
                        //Exceptions
                        else
                        {
                            TcpHeader = String.Format("{0:X}{1:X}   {2:X}{3:X}   {4:X}{5:X}", frame[0], frame[1], frame[2], frame[3], frame[4], frame[5]);
                            modbusHeader = String.Format("{0:X}   {1:X}   {2:X}", frame[6], frame[7], frame[8]);
                            return TcpHeader + "   " + modbusHeader;
                        }

                    case NumberStyles.Integer:
                        //Read Area
                        if (frame[7] <= 4)
                        {
                            TcpHeader = String.Format("{0}{1}   {2}{3}   {4}{5}", frame[0], frame[1], frame[2], frame[3], frame[4], frame[5]);
                            modbusHeader = String.Format("{0}   {1}   {2}", frame[6], frame[7], frame[8]);
                            restData = frame.ToList().GetRange(10, frame.Length - 10).Aggregate(String.Format("{0}", frame[9]), (sum, next) => sum + " " + String.Format("{0}", next));
                            return TcpHeader + "   " + modbusHeader + "   " + restData;
                        }
                        //Single Cols and Single Register
                        else if (frame[7] == 5 || frame[7] == 6)
                        {
                            TcpHeader = String.Format("{0}{1}   {2}{3}   {4}{5}", frame[0], frame[1], frame[2], frame[3], frame[4], frame[5]);
                            modbusHeader = String.Format("{0}   {1}   {2}{3}   {4} {5}", frame[6], frame[7], frame[8], frame[9], frame[10], frame[11]);
                            return TcpHeader + "   " + modbusHeader;
                        }
                        //Multiple Coils and Multiple Registers
                        else if (frame[7] == 15 || frame[7] == 16)
                        {
                            TcpHeader = String.Format("{0}{1}   {2}{3}   {4}{5}", frame[0], frame[1], frame[2], frame[3], frame[4], frame[5]);
                            modbusHeader = String.Format("{0}   {1}   {2}{3}   {4}{5}", frame[6], frame[7], frame[8], frame[9], frame[10], frame[11]);
                            return TcpHeader + "   " + modbusHeader;
                        }
                        //Exceptions
                        else
                        {
                            TcpHeader = String.Format("{0}{1}   {2}{3}   {4}{5}", frame[0], frame[1], frame[2], frame[3], frame[4], frame[5]);
                            modbusHeader = String.Format("{0}   {1}   {2}", frame[6], frame[7], frame[8]);
                            return TcpHeader + "   " + modbusHeader;
                        }

                    default:
                        //Read Area
                        if (frame[7] <= 4)
                        {
                            TcpHeader = String.Format("{0}{1}   {2}{3}   {4}{5}", frame[0], frame[1], frame[2], frame[3], frame[4], frame[5]);
                            modbusHeader = String.Format("{0}   {1}   {2}", frame[6], frame[7], frame[8]);
                            restData = frame.ToList().GetRange(10, frame.Length - 10).Aggregate(String.Format("{0}", frame[9]), (sum, next) => sum + " " + String.Format("{0}", next));
                            return TcpHeader + "   " + modbusHeader + "   " + restData;
                        }
                        //Single Cols and Single Register
                        else if (frame[7] == 5 || frame[7] == 6)
                        {
                            TcpHeader = String.Format("{0}{1}   {2}{3}   {4}{5}", frame[0], frame[1], frame[2], frame[3], frame[4], frame[5]);
                            modbusHeader = String.Format("{0}   {1}   {2}{3}   {4} {5}", frame[6], frame[7], frame[8], frame[9], frame[10], frame[11]);
                            return TcpHeader + "   " + modbusHeader;
                        }
                        //Multiple Coils and Multiple Registers
                        else if (frame[7] == 15 || frame[7] == 16)
                        {
                            TcpHeader = String.Format("{0}{1}   {2}{3}   {4}{5}", frame[0], frame[1], frame[2], frame[3], frame[4], frame[5]);
                            modbusHeader = String.Format("{0}   {1}   {2}{3}   {4}{5}", frame[6], frame[7], frame[8], frame[9], frame[10], frame[11]);
                            return TcpHeader + "   " + modbusHeader;
                        }
                        //Exceptions
                        else
                        {
                            TcpHeader = String.Format("{0}{1}   {2}{3}   {4}{5}", frame[0], frame[1], frame[2], frame[3], frame[4], frame[5]);
                            modbusHeader = String.Format("{0}   {1}   {2}", frame[6], frame[7], frame[8]);
                            return TcpHeader + "   " + modbusHeader;
                        }
                }
            }
            catch (Exception Ex)
            {
                if (sendInfoEv != null)
                    sendInfoEv(this, new ProjectEventArgs(frame, DateTime.Now, "ModbusMasterTCP.FormatFrameResponse :" + Ex.Message));

                return "Conversion Foult";
            }
        }

        /// <summary>
        /// Zapytanie jednostkowe bajtami danych
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        byte[] IDriverModel.sendBytes(byte[] data)
        {
            return null;
        }

        /// <summary>
        /// Steronik zajety
        /// </summary>
        bool IDriverModel.isBusy
        {
            get { return bWorker.IsBusy; }
        }

        /// <summary>
        /// Rozne parametry
        /// 1- Wlazenie rozszerzonej adresacji dla Device
        /// 2- Wlaczenie Block Adress
        /// </summary>
        bool[] IDriverModel.AuxParam
        {
            get { return new Boolean[] { true, false }; }
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
                    // TODO: dispose managed state (managed objects).
                    tcpClient.Dispose();
                    bWorker.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }

        #endregion IDisposable Support
    }
}