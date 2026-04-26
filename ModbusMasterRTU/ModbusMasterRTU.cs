using ProjectDataLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO.Ports;
using System.Linq;
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

        //Informacja o aktywnosci odczytu
        private Boolean isLive_;

        public bool isLive
        {
            get { return isLive_; }
            set
            {
                isLive_ = value;
            }
        }

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

        private Guid ObjId_;

        Guid IDriverModel.ObjId
        {
            get { return ObjId_; }
        }

        /// <summary>
        /// Zapytanie do urzadzen zdalnych
        /// </summary>
        private List<byte[]> mainFrames = new List<byte[]>();

        /// <summary>
        /// Serialport
        /// </summary>
        private SerialPort sPort;

        /// <summary>
        /// Parametry Transmisji
        /// </summary>
        private IoDriverParam DriverParam_;

        /// <summary>
        /// Nazwa Sterownika
        /// </summary>
        string IDriverModel.driverName
        {
            get { return "ModbusMasterRTU"; }
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

                List<Tag> tgs = (from c in tagsList1 where c is Tag select (Tag)c).ToList();

                this.tagList.AddRange(tgs);

                //Konfiguracja danych
                ConfigData(tagList);

                //Nowy client
                sPort = new SerialPort(DriverParam_.PortName, DriverParam_.BoundRate, DriverParam_.parity, DriverParam_.DataBits, DriverParam_.stopBits);
                sPort.Open();

                //Start obiegu
                bWorker.RunWorkerAsync();

                //Comm jest aktywna
                isLive = true;

                foreach (IDriverModel it in this.tagList)
                    it.isAlive = true;

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
        /// Dodanie tagow
        /// </summary>
        /// <param name="tagList"></param>
        /// <returns></returns>
        bool IDriverModel.addTagsComm(List<ITag> tagList1)
        {
            try
            {
                List<Tag> tgs = (from c in tagList1 where c is Tag select (Tag)c).ToList();

                //Dodanie Tagow
                this.tagList.AddRange(tgs);

                //Usuniecie duplikatu
                //this.tagList = this.tagList.Distinct().ToList();

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
                List<Tag> tgs = (from c in tagList2 where c is Tag select (Tag)c).ToList();

                //usuniecie danych
                foreach (Tag tn in tgs)
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
                DriverParam_ = new IoDriverParam();

                //Id
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
                    errorSendEv(this, new ProjectEventArgs(new byte[] { 0 }, DateTime.Now, "ModbusMasterRTU.Constructor :" + Ex.Message, Ex));
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
            if (!sPort.IsOpen)
                return;

            //Wysalnie zapytan
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
                        byte[] wrCoFrame = CreateWriteHeader((byte)tg.deviceAdress, (ushort)tg.startData, (ushort)tg.coreDataSend.Length, buffByteArr, fctWriteMultipleCoils);
                        tg.setMod = false;

                        //Zdarzenia
                        if (sendLogInfoEv != null)
                            sendLogInfoEv(this, new ProjectEventArgs(wrCoFrame, DateTime.Now, getAreaFromFrame(wrCoFrame) + "REQUEST"));

                        //Wyslanie i oczekiwanie na zwrot Danych
                        byte[] rcvCo = writeDataOnPort(wrCoFrame);

                        if (rcvCo != null)
                        {
                            //Wyslanie zdarzenia o nadejsciu danych
                            if (reciveLogInfoEv != null)
                                reciveLogInfoEv(this, new ProjectEventArgs(rcvCo, DateTime.Now, getAreaFromFrame(rcvCo) + "RESPONSE"));

                            //Sprawdzenie
                            if (checkResponse(wrCoFrame, rcvCo))
                            {
                            }
                        }
                    }
                }
                catch (Exception Ex)
                {
                    if (errorSendEv != null)
                        errorSendEv(this, new ProjectEventArgs(new byte[] { 0 }, DateTime.Now, "ModbusMasterRTU.bWorker_DoWork.WriteCO :" + Ex.Message, Ex));
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
                        byte[] wrHRFrame = CreateWriteHeader((byte)tg.deviceAdress, (ushort)tg.startData, 0, buffByteArr, fctWriteMultipleRegister);
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
                            }
                        }
                    }
                }
                catch (Exception Ex)
                {
                    if (errorSendEv != null)
                        errorSendEv(this, new ProjectEventArgs(new byte[] { 0 }, DateTime.Now, "ModbusMasterRTU.bWorker_DoWork.WriteHR :" + Ex.Message, Ex));
                }
            }

            #endregion Zapisz dane na porcie

            //Opoznienie wątku o dany czas
            Thread.Sleep(DriverParam_.ReplyTime);
        }

        /// <summary>
        /// Zapisuje dane na porcie
        /// </summary>
        /// <param name="dane"></param>
        /// <returns></returns>
        private byte[] writeDataOnPort(byte[] dane)
        {
            //Zapis na port
            sPort.Write(dane, 0, dane.Length);

            //Dane do otczytu
            int count = responseDataCount(dane);

            //Czekanie na odpowiedz
            for (int i = 0; i < DriverParam_.Timeout; i = i + 5)
            {
                //Nadeszły dane
                if (sPort.BytesToRead >= count)
                {
                    //Buffor na dane
                    byte[] Response = new byte[count];
                    sPort.Read(Response, 0, Response.Length);
                    return Response;
                }

                //Opoznienie watku
                Thread.Sleep(5);
            }

            //Odczyt kodu bledu
            if (sPort.BytesToRead == 5)
            {
                byte[] response = new byte[5];
                sPort.Read(response, 0, response.Length);
                return response;
            }

            //Jezeli program dojedzie do tego miejsca mamy overtime
            return null;
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
                    int startIndex = BitConverter.ToUInt16(new byte[] { frRequest[3], frRequest[2] }, 0);
                    int countData = BitConverter.ToUInt16(new byte[] { frRequest[5], frRequest[4] }, 0);

                    //Zamiana memoryArea ze struktury byte[] na Boolean[]. Odfiltrowanie danych
                    BitArray btArr = new BitArray(getDataFrame(frResponse, countData).ToArray());
                    Boolean[] bitArray = new Boolean[btArr.Length];
                    //Przekopiowanie bitow do tablicy pomocniczej
                    btArr.CopyTo(bitArray, 0);

                    //Obszar pamiecie
                    MemoryAreaInfo mInfo = ((IDriverModel)this).MemoryAreaInf.Where(x => x.fctCode == frResponse[1]).ToArray()[0];

                    //Selekcja tagow do przeslania do odswierzenia
                    var zm = from n in tagList
                             where n.deviceAdress == frResponse[0] && n.areaData == mInfo.Name && n.bitAdres >= startIndex * mInfo.AdresSize && n.bitAdres + n.coreData.Length <= (startIndex + countData) * mInfo.AdresSize
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

            return false;
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
                Byte[] header = dane.ToList().GetRange(0, 3).ToArray();

                //Wyslanie ramki
                return dane.ToList().GetRange(3, header[2]);
            }
            catch (Exception Ex)
            {
                if (errorSendEv != null)
                    errorSendEv(this, new ProjectEventArgs(new byte[] { 0 }, DateTime.Now, "ModbusMasterRTU.getDataFrame :" + Ex.Message, Ex));

                return null;
            }
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
                    sPort.Close();
                }
            }
            catch (Exception Ex)
            {
                if (errorSendEv != null)
                    errorSendEv(this, new ProjectEventArgs(new byte[] { 0 }, DateTime.Now, "ModbusMasterRTU.bWorker_RunWorkerCompleted :" + Ex.Message, Ex));
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
                        //Zabezpiecznie dlugiej ramki
                        if (points[points.Count - 1].Y - points[points.Count - 1].X >= maxFrameSizeCO)
                        {
                            //Jestemy w miejscu Taga trzeba podac koniec
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

                        //Zabezpieczenie przed przekroczeniem wolnego odstepu
                        if (marker == maxSizeCO)
                            points.Add(new PO(i, 0));

                        //Sprawdzenie czy tagi sa w zakresie
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

        /// <summary>
        /// Stworzenie nagłówka dla odczytu
        /// </summary>
        /// <param name="id"></param>
        /// <param name="startAddress"></param>
        /// <param name="length"></param>
        /// <param name="function"></param>
        /// <returns></returns>
        private byte[] CreateReadHeader(byte id, ushort startAddress, ushort length, byte function)
        {
            //tworzenie tablicy
            byte[] data = new byte[8];

            data[0] = id; //id
            data[1] = function; // kod funkcji

            //Start
            byte[] start_adr = BitConverter.GetBytes(startAddress);
            data[2] = start_adr[1];
            data[3] = start_adr[0];

            //ilosc danych
            byte[] length_b = BitConverter.GetBytes(length);
            data[4] = length_b[1];
            data[5] = length_b[0];

            //Obloczenia CRC
            int crc_result = crc16(data, 6);
            byte[] crc_bytes = BitConverter.GetBytes(crc_result);

            data[6] = crc_bytes[1];
            data[7] = crc_bytes[0];

            return data;
        }

        /// <summary>
        /// Stworzenie nagłowka dla zapisu
        /// </summary>
        /// <param name="id"></param>
        /// <param name="startAddress"></param>
        /// <param name="value"></param>
        /// <param name="function"></param>
        /// <returns></returns>
        private byte[] CreateWriteHeader(byte id, ushort startAddress, ushort countData, byte[] value, byte function)
        {
            if (function == fctWriteSingleCoil)
            {
                //tworzenie tablicy
                byte[] data = new byte[8];

                data[0] = id; //id
                data[1] = function; // kod funkcji

                //Start
                byte[] start_adr = BitConverter.GetBytes(startAddress);
                data[2] = start_adr[1];
                data[3] = start_adr[0];

                //ilosc danych
                data[4] = value[0];
                data[5] = 0x00;

                //Obloczenia CRC
                int crc_result = crc16(data, data.Length - 2);
                byte[] crc_bytes = BitConverter.GetBytes(crc_result);

                data[6] = crc_bytes[1];
                data[7] = crc_bytes[0];

                return data;
            }

            if (function == fctWriteMultipleCoils)
            {
                //tworzenie tablicy
                byte[] data = new byte[9 + value.Length];

                data[0] = (Byte)id; //id
                data[1] = function; // kod funkcji

                //Start
                byte[] start_adr = BitConverter.GetBytes(startAddress);
                data[2] = start_adr[1];
                data[3] = start_adr[0];

                //ilosc danych
                byte[] dataL = BitConverter.GetBytes(countData);
                data[4] = dataL[1];
                data[5] = dataL[0];

                //ilość następnych bajtów
                data[6] = (byte)value.Length;

                //Zapisanie bajtów
                for (int i = 0; i < value.Length; i++)
                    data[7 + i] = value[i];

                //Obloczenia CRC
                int crc_result = crc16(data, data.Length - 2);
                byte[] crc_bytes = BitConverter.GetBytes(crc_result);

                data[data.Length - 2] = crc_bytes[1];
                data[data.Length - 1] = crc_bytes[0];

                return data;
            }

            if (function == fctWriteSingleRegister)
            {
                //tworzenie tablicy
                byte[] data = new byte[8];

                data[0] = (Byte)id; //id
                data[1] = function; // kod funkcji

                //Start
                byte[] start_adr = BitConverter.GetBytes(startAddress);
                data[2] = start_adr[1];
                data[3] = start_adr[0];

                //ilosc danych
                data[4] = value[1];
                data[5] = value[0];

                //Obloczenia CRC
                int crc_result = crc16(data, data.Length - 2);
                byte[] crc_bytes = BitConverter.GetBytes(crc_result);

                data[6] = crc_bytes[1];
                data[7] = crc_bytes[0];

                return data;
            }

            if (function == fctWriteMultipleRegister)
            {
                //tworzenie tablicy
                byte[] data = new byte[9 + value.Length];

                data[0] = (Byte)id; //id
                data[1] = function; // kod funkcji

                //Start
                byte[] start_adr = BitConverter.GetBytes(startAddress);
                data[2] = start_adr[1];
                data[3] = start_adr[0];

                //ilosc rejesterow
                data[4] = 0x00;
                data[5] = (byte)(value.Length / 2);

                //ilość następnych bajtów
                data[6] = (byte)value.Length;

                //Zapisanie bajtów
                for (int i = 0; i < value.Length; i++)
                    data[7 + i] = value[i];

                //Obloczenia CRC
                int crc_result = crc16(data, data.Length - 2);
                byte[] crc_bytes = BitConverter.GetBytes(crc_result);

                data[data.Length - 2] = crc_bytes[1];
                data[data.Length - 1] = crc_bytes[0];

                return data;
            }
            else
                return null;
        }

        /// <summary>
        /// Crc16 calculation
        /// </summary>
        /// <param name="modbusframe"></param>
        /// <param name="Length"></param>
        /// <returns></returns>
        private int crc16(byte[] modbusframe, int Length)
        {
            byte[] crc_table = new byte[512];

            #region tablica CRC

            crc_table[0] = 0x0;
            crc_table[1] = 0xC1;
            crc_table[2] = 0x81;
            crc_table[3] = 0x40;
            crc_table[4] = 0x1;
            crc_table[5] = 0xC0;
            crc_table[6] = 0x80;
            crc_table[7] = 0x41;
            crc_table[8] = 0x1;
            crc_table[9] = 0xC0;
            crc_table[10] = 0x80;
            crc_table[11] = 0x41;
            crc_table[12] = 0x0;
            crc_table[13] = 0xC1;
            crc_table[14] = 0x81;
            crc_table[15] = 0x40;
            crc_table[16] = 0x1;
            crc_table[17] = 0xC0;
            crc_table[18] = 0x80;
            crc_table[19] = 0x41;
            crc_table[20] = 0x0;
            crc_table[21] = 0xC1;
            crc_table[22] = 0x81;
            crc_table[23] = 0x40;
            crc_table[24] = 0x0;
            crc_table[25] = 0xC1;
            crc_table[26] = 0x81;
            crc_table[27] = 0x40;
            crc_table[28] = 0x1;
            crc_table[29] = 0xC0;
            crc_table[30] = 0x80;
            crc_table[31] = 0x41;
            crc_table[32] = 0x1;
            crc_table[33] = 0xC0;
            crc_table[34] = 0x80;
            crc_table[35] = 0x41;
            crc_table[36] = 0x0;
            crc_table[37] = 0xC1;
            crc_table[38] = 0x81;
            crc_table[39] = 0x40;
            crc_table[40] = 0x0;
            crc_table[41] = 0xC1;
            crc_table[42] = 0x81;
            crc_table[43] = 0x40;
            crc_table[44] = 0x1;
            crc_table[45] = 0xC0;
            crc_table[46] = 0x80;
            crc_table[47] = 0x41;
            crc_table[48] = 0x0;
            crc_table[49] = 0xC1;
            crc_table[50] = 0x81;
            crc_table[51] = 0x40;
            crc_table[52] = 0x1;
            crc_table[53] = 0xC0;
            crc_table[54] = 0x80;
            crc_table[55] = 0x41;
            crc_table[56] = 0x1;
            crc_table[57] = 0xC0;
            crc_table[58] = 0x80;
            crc_table[59] = 0x41;
            crc_table[60] = 0x0;
            crc_table[61] = 0xC1;
            crc_table[62] = 0x81;
            crc_table[63] = 0x40;
            crc_table[64] = 0x1;
            crc_table[65] = 0xC0;
            crc_table[66] = 0x80;
            crc_table[67] = 0x41;
            crc_table[68] = 0x0;
            crc_table[69] = 0xC1;
            crc_table[70] = 0x81;
            crc_table[71] = 0x40;
            crc_table[72] = 0x0;
            crc_table[73] = 0xC1;
            crc_table[74] = 0x81;
            crc_table[75] = 0x40;
            crc_table[76] = 0x1;
            crc_table[77] = 0xC0;
            crc_table[78] = 0x80;
            crc_table[79] = 0x41;
            crc_table[80] = 0x0;
            crc_table[81] = 0xC1;
            crc_table[82] = 0x81;
            crc_table[83] = 0x40;
            crc_table[84] = 0x1;
            crc_table[85] = 0xC0;
            crc_table[86] = 0x80;
            crc_table[87] = 0x41;
            crc_table[88] = 0x1;
            crc_table[89] = 0xC0;
            crc_table[90] = 0x80;
            crc_table[91] = 0x41;
            crc_table[92] = 0x0;
            crc_table[93] = 0xC1;
            crc_table[94] = 0x81;
            crc_table[95] = 0x40;
            crc_table[96] = 0x0;
            crc_table[97] = 0xC1;
            crc_table[98] = 0x81;
            crc_table[99] = 0x40;
            crc_table[100] = 0x1;
            crc_table[101] = 0xC0;
            crc_table[102] = 0x80;
            crc_table[103] = 0x41;
            crc_table[104] = 0x1;
            crc_table[105] = 0xC0;
            crc_table[106] = 0x80;
            crc_table[107] = 0x41;
            crc_table[108] = 0x0;
            crc_table[109] = 0xC1;
            crc_table[110] = 0x81;
            crc_table[111] = 0x40;
            crc_table[112] = 0x1;
            crc_table[113] = 0xC0;
            crc_table[114] = 0x80;
            crc_table[115] = 0x41;
            crc_table[116] = 0x0;
            crc_table[117] = 0xC1;
            crc_table[118] = 0x81;
            crc_table[119] = 0x40;
            crc_table[120] = 0x0;
            crc_table[121] = 0xC1;
            crc_table[122] = 0x81;
            crc_table[123] = 0x40;
            crc_table[124] = 0x1;
            crc_table[125] = 0xC0;
            crc_table[126] = 0x80;
            crc_table[127] = 0x41;
            crc_table[128] = 0x1;
            crc_table[129] = 0xC0;
            crc_table[130] = 0x80;
            crc_table[131] = 0x41;
            crc_table[132] = 0x0;
            crc_table[133] = 0xC1;
            crc_table[134] = 0x81;
            crc_table[135] = 0x40;
            crc_table[136] = 0x0;
            crc_table[137] = 0xC1;
            crc_table[138] = 0x81;
            crc_table[139] = 0x40;
            crc_table[140] = 0x1;
            crc_table[141] = 0xC0;
            crc_table[142] = 0x80;
            crc_table[143] = 0x41;
            crc_table[144] = 0x0;
            crc_table[145] = 0xC1;
            crc_table[146] = 0x81;
            crc_table[147] = 0x40;
            crc_table[148] = 0x1;
            crc_table[149] = 0xC0;
            crc_table[150] = 0x80;
            crc_table[151] = 0x41;
            crc_table[152] = 0x1;
            crc_table[153] = 0xC0;
            crc_table[154] = 0x80;
            crc_table[155] = 0x41;
            crc_table[156] = 0x0;
            crc_table[157] = 0xC1;
            crc_table[158] = 0x81;
            crc_table[159] = 0x40;
            crc_table[160] = 0x0;
            crc_table[161] = 0xC1;
            crc_table[162] = 0x81;
            crc_table[163] = 0x40;
            crc_table[164] = 0x1;
            crc_table[165] = 0xC0;
            crc_table[166] = 0x80;
            crc_table[167] = 0x41;
            crc_table[168] = 0x1;
            crc_table[169] = 0xC0;
            crc_table[170] = 0x80;
            crc_table[171] = 0x41;
            crc_table[172] = 0x0;
            crc_table[173] = 0xC1;
            crc_table[174] = 0x81;
            crc_table[175] = 0x40;
            crc_table[176] = 0x1;
            crc_table[177] = 0xC0;
            crc_table[178] = 0x80;
            crc_table[179] = 0x41;
            crc_table[180] = 0x0;
            crc_table[181] = 0xC1;
            crc_table[182] = 0x81;
            crc_table[183] = 0x40;
            crc_table[184] = 0x0;
            crc_table[185] = 0xC1;
            crc_table[186] = 0x81;
            crc_table[187] = 0x40;
            crc_table[188] = 0x1;
            crc_table[189] = 0xC0;
            crc_table[190] = 0x80;
            crc_table[191] = 0x41;
            crc_table[192] = 0x0;
            crc_table[193] = 0xC1;
            crc_table[194] = 0x81;
            crc_table[195] = 0x40;
            crc_table[196] = 0x1;
            crc_table[197] = 0xC0;
            crc_table[198] = 0x80;
            crc_table[199] = 0x41;
            crc_table[200] = 0x1;
            crc_table[201] = 0xC0;
            crc_table[202] = 0x80;
            crc_table[203] = 0x41;
            crc_table[204] = 0x0;
            crc_table[205] = 0xC1;
            crc_table[206] = 0x81;
            crc_table[207] = 0x40;
            crc_table[208] = 0x1;
            crc_table[209] = 0xC0;
            crc_table[210] = 0x80;
            crc_table[211] = 0x41;
            crc_table[212] = 0x0;
            crc_table[213] = 0xC1;
            crc_table[214] = 0x81;
            crc_table[215] = 0x40;
            crc_table[216] = 0x0;
            crc_table[217] = 0xC1;
            crc_table[218] = 0x81;
            crc_table[219] = 0x40;
            crc_table[220] = 0x1;
            crc_table[221] = 0xC0;
            crc_table[222] = 0x80;
            crc_table[223] = 0x41;
            crc_table[224] = 0x1;
            crc_table[225] = 0xC0;
            crc_table[226] = 0x80;
            crc_table[227] = 0x41;
            crc_table[228] = 0x0;
            crc_table[229] = 0xC1;
            crc_table[230] = 0x81;
            crc_table[231] = 0x40;
            crc_table[232] = 0x0;
            crc_table[233] = 0xC1;
            crc_table[234] = 0x81;
            crc_table[235] = 0x40;
            crc_table[236] = 0x1;
            crc_table[237] = 0xC0;
            crc_table[238] = 0x80;
            crc_table[239] = 0x41;
            crc_table[240] = 0x0;
            crc_table[241] = 0xC1;
            crc_table[242] = 0x81;
            crc_table[243] = 0x40;
            crc_table[244] = 0x1;
            crc_table[245] = 0xC0;
            crc_table[246] = 0x80;
            crc_table[247] = 0x41;
            crc_table[248] = 0x1;
            crc_table[249] = 0xC0;
            crc_table[250] = 0x80;
            crc_table[251] = 0x41;
            crc_table[252] = 0x0;
            crc_table[253] = 0xC1;
            crc_table[254] = 0x81;
            crc_table[255] = 0x40;
            crc_table[256] = 0x0;
            crc_table[257] = 0xC0;
            crc_table[258] = 0xC1;
            crc_table[259] = 0x1;
            crc_table[260] = 0xC3;
            crc_table[261] = 0x3;
            crc_table[262] = 0x2;
            crc_table[263] = 0xC2;
            crc_table[264] = 0xC6;
            crc_table[265] = 0x6;
            crc_table[266] = 0x7;
            crc_table[267] = 0xC7;
            crc_table[268] = 0x5;
            crc_table[269] = 0xC5;
            crc_table[270] = 0xC4;
            crc_table[271] = 0x4;
            crc_table[272] = 0xCC;
            crc_table[273] = 0xC;
            crc_table[274] = 0xD;
            crc_table[275] = 0xCD;
            crc_table[276] = 0xF;
            crc_table[277] = 0xCF;
            crc_table[278] = 0xCE;
            crc_table[279] = 0xE;
            crc_table[280] = 0xA;
            crc_table[281] = 0xCA;
            crc_table[282] = 0xCB;
            crc_table[283] = 0xB;
            crc_table[284] = 0xC9;
            crc_table[285] = 0x9;
            crc_table[286] = 0x8;
            crc_table[287] = 0xC8;
            crc_table[288] = 0xD8;
            crc_table[289] = 0x18;
            crc_table[290] = 0x19;
            crc_table[291] = 0xD9;
            crc_table[292] = 0x1B;
            crc_table[293] = 0xDB;
            crc_table[294] = 0xDA;
            crc_table[295] = 0x1A;
            crc_table[296] = 0x1E;
            crc_table[297] = 0xDE;
            crc_table[298] = 0xDF;
            crc_table[299] = 0x1F;
            crc_table[300] = 0xDD;
            crc_table[301] = 0x1D;
            crc_table[302] = 0x1C;
            crc_table[303] = 0xDC;
            crc_table[304] = 0x14;
            crc_table[305] = 0xD4;
            crc_table[306] = 0xD5;
            crc_table[307] = 0x15;
            crc_table[308] = 0xD7;
            crc_table[309] = 0x17;
            crc_table[310] = 0x16;
            crc_table[311] = 0xD6;
            crc_table[312] = 0xD2;
            crc_table[313] = 0x12;
            crc_table[314] = 0x13;
            crc_table[315] = 0xD3;
            crc_table[316] = 0x11;
            crc_table[317] = 0xD1;
            crc_table[318] = 0xD0;
            crc_table[319] = 0x10;
            crc_table[320] = 0xF0;
            crc_table[321] = 0x30;
            crc_table[322] = 0x31;
            crc_table[323] = 0xF1;
            crc_table[324] = 0x33;
            crc_table[325] = 0xF3;
            crc_table[326] = 0xF2;
            crc_table[327] = 0x32;
            crc_table[328] = 0x36;
            crc_table[329] = 0xF6;
            crc_table[330] = 0xF7;
            crc_table[331] = 0x37;
            crc_table[332] = 0xF5;
            crc_table[333] = 0x35;
            crc_table[334] = 0x34;
            crc_table[335] = 0xF4;
            crc_table[336] = 0x3C;
            crc_table[337] = 0xFC;
            crc_table[338] = 0xFD;
            crc_table[339] = 0x3D;
            crc_table[340] = 0xFF;
            crc_table[341] = 0x3F;
            crc_table[342] = 0x3E;
            crc_table[343] = 0xFE;
            crc_table[344] = 0xFA;
            crc_table[345] = 0x3A;
            crc_table[346] = 0x3B;
            crc_table[347] = 0xFB;
            crc_table[348] = 0x39;
            crc_table[349] = 0xF9;
            crc_table[350] = 0xF8;
            crc_table[351] = 0x38;
            crc_table[352] = 0x28;
            crc_table[353] = 0xE8;
            crc_table[354] = 0xE9;
            crc_table[355] = 0x29;
            crc_table[356] = 0xEB;
            crc_table[357] = 0x2B;
            crc_table[358] = 0x2A;
            crc_table[359] = 0xEA;
            crc_table[360] = 0xEE;
            crc_table[361] = 0x2E;
            crc_table[362] = 0x2F;
            crc_table[363] = 0xEF;
            crc_table[364] = 0x2D;
            crc_table[365] = 0xED;
            crc_table[366] = 0xEC;
            crc_table[367] = 0x2C;
            crc_table[368] = 0xE4;
            crc_table[369] = 0x24;
            crc_table[370] = 0x25;
            crc_table[371] = 0xE5;
            crc_table[372] = 0x27;
            crc_table[373] = 0xE7;
            crc_table[374] = 0xE6;
            crc_table[375] = 0x26;
            crc_table[376] = 0x22;
            crc_table[377] = 0xE2;
            crc_table[378] = 0xE3;
            crc_table[379] = 0x23;
            crc_table[380] = 0xE1;
            crc_table[381] = 0x21;
            crc_table[382] = 0x20;
            crc_table[383] = 0xE0;
            crc_table[384] = 0xA0;
            crc_table[385] = 0x60;
            crc_table[386] = 0x61;
            crc_table[387] = 0xA1;
            crc_table[388] = 0x63;
            crc_table[389] = 0xA3;
            crc_table[390] = 0xA2;
            crc_table[391] = 0x62;
            crc_table[392] = 0x66;
            crc_table[393] = 0xA6;
            crc_table[394] = 0xA7;
            crc_table[395] = 0x67;
            crc_table[396] = 0xA5;
            crc_table[397] = 0x65;
            crc_table[398] = 0x64;
            crc_table[399] = 0xA4;
            crc_table[400] = 0x6C;
            crc_table[401] = 0xAC;
            crc_table[402] = 0xAD;
            crc_table[403] = 0x6D;
            crc_table[404] = 0xAF;
            crc_table[405] = 0x6F;
            crc_table[406] = 0x6E;
            crc_table[407] = 0xAE;
            crc_table[408] = 0xAA;
            crc_table[409] = 0x6A;
            crc_table[410] = 0x6B;
            crc_table[411] = 0xAB;
            crc_table[412] = 0x69;
            crc_table[413] = 0xA9;
            crc_table[414] = 0xA8;
            crc_table[415] = 0x68;
            crc_table[416] = 0x78;
            crc_table[417] = 0xB8;
            crc_table[418] = 0xB9;
            crc_table[419] = 0x79;
            crc_table[420] = 0xBB;
            crc_table[421] = 0x7B;
            crc_table[422] = 0x7A;
            crc_table[423] = 0xBA;
            crc_table[424] = 0xBE;
            crc_table[425] = 0x7E;
            crc_table[426] = 0x7F;
            crc_table[427] = 0xBF;
            crc_table[428] = 0x7D;
            crc_table[429] = 0xBD;
            crc_table[430] = 0xBC;
            crc_table[431] = 0x7C;
            crc_table[432] = 0xB4;
            crc_table[433] = 0x74;
            crc_table[434] = 0x75;
            crc_table[435] = 0xB5;
            crc_table[436] = 0x77;
            crc_table[437] = 0xB7;
            crc_table[438] = 0xB6;
            crc_table[439] = 0x76;
            crc_table[440] = 0x72;
            crc_table[441] = 0xB2;
            crc_table[442] = 0xB3;
            crc_table[443] = 0x73;
            crc_table[444] = 0xB1;
            crc_table[445] = 0x71;
            crc_table[446] = 0x70;
            crc_table[447] = 0xB0;
            crc_table[448] = 0x50;
            crc_table[449] = 0x90;
            crc_table[450] = 0x91;
            crc_table[451] = 0x51;
            crc_table[452] = 0x93;
            crc_table[453] = 0x53;
            crc_table[454] = 0x52;
            crc_table[455] = 0x92;
            crc_table[456] = 0x96;
            crc_table[457] = 0x56;
            crc_table[458] = 0x57;
            crc_table[459] = 0x97;
            crc_table[460] = 0x55;
            crc_table[461] = 0x95;
            crc_table[462] = 0x94;
            crc_table[463] = 0x54;
            crc_table[464] = 0x9C;
            crc_table[465] = 0x5C;
            crc_table[466] = 0x5D;
            crc_table[467] = 0x9D;
            crc_table[468] = 0x5F;
            crc_table[469] = 0x9F;
            crc_table[470] = 0x9E;
            crc_table[471] = 0x5E;
            crc_table[472] = 0x5A;
            crc_table[473] = 0x9A;
            crc_table[474] = 0x9B;
            crc_table[475] = 0x5B;
            crc_table[476] = 0x99;
            crc_table[477] = 0x59;
            crc_table[478] = 0x58;
            crc_table[479] = 0x98;
            crc_table[480] = 0x88;
            crc_table[481] = 0x48;
            crc_table[482] = 0x49;
            crc_table[483] = 0x89;
            crc_table[484] = 0x4B;
            crc_table[485] = 0x8B;
            crc_table[486] = 0x8A;
            crc_table[487] = 0x4A;
            crc_table[488] = 0x4E;
            crc_table[489] = 0x8E;
            crc_table[490] = 0x8F;
            crc_table[491] = 0x4F;
            crc_table[492] = 0x8D;
            crc_table[493] = 0x4D;
            crc_table[494] = 0x4C;
            crc_table[495] = 0x8C;
            crc_table[496] = 0x44;
            crc_table[497] = 0x84;
            crc_table[498] = 0x85;
            crc_table[499] = 0x45;
            crc_table[500] = 0x87;
            crc_table[501] = 0x47;
            crc_table[502] = 0x46;
            crc_table[503] = 0x86;
            crc_table[504] = 0x82;
            crc_table[505] = 0x42;
            crc_table[506] = 0x43;
            crc_table[507] = 0x83;
            crc_table[508] = 0x41;
            crc_table[509] = 0x81;
            crc_table[510] = 0x80;
            crc_table[511] = 0x40;

            #endregion tablica CRC

            int i;
            int index;
            int crc_Low = 0xFF;
            int crc_High = 0xFF;

            for (i = 0; i < Length; i++)
            {
                index = crc_High ^ (char)modbusframe[i];
                crc_High = crc_Low ^ crc_table[index];
                crc_Low = (byte)crc_table[index + 256];
            }

            return crc_High * 256 + crc_Low;
        }

        /// <summary>
        /// Sparwdzenie odpowiedzi
        /// </summary>
        /// <param name="zapytanie"></param>
        /// <param name="odpowiedz"></param>
        /// <returns></returns>
        private Boolean checkResponse(byte[] zapytanie, byte[] odpowiedz)
        {
            try
            {
                //CRC
                int crc_result = crc16(odpowiedz, odpowiedz.Length - 2);
                byte[] crc_bytes = BitConverter.GetBytes(crc_result);
                if (odpowiedz[odpowiedz.Length - 1] != crc_bytes[0] || odpowiedz[odpowiedz.Length - 2] != crc_bytes[1])
                    throw new ApplicationException(String.Format("CRC problem for function: {0}", odpowiedz[1]));

                //Normalna odpowiedz
                if (odpowiedz.Length > 5)
                {
                    //Testujemy ID
                    if (zapytanie[0] != odpowiedz[0])
                        throw new ApplicationException("ID - response isnt correct.");

                    //Testujemy FCT
                    if (zapytanie[1] != odpowiedz[1])
                        throw new ApplicationException("Response code function isnt correct.");

                    #region Read CO i DI

                    //Read DI and CO
                    if (zapytanie[1] == 1 || zapytanie[1] == 2)
                    {
                        //DI CO
                        int countReq = BitConverter.ToInt16(new byte[2] { zapytanie[5], zapytanie[4] }, 0);
                        int reszta = (int)(float)(countReq % 8);
                        int byteCount = countReq / 8;

                        //Sprawdzenie czy jest reszta
                        if (reszta > 0)
                            byteCount++;

                        //Sprawdzanie odpowiedzi
                        if (byteCount != odpowiedz[2])
                            throw new ApplicationException("Response length isnt appropriate fct: 1, 2.");

                        //Sprawdznie czy pole dlugosć odpowiedzi odzwierciedla stan faktyczny
                        if (odpowiedz.Length != odpowiedz[2] + 5)
                            throw new ApplicationException("Remote device problem. Response length isnt appropriate fct: 1, 2.");
                    }

                    #endregion Read CO i DI

                    #region Read HR i DI

                    if (zapytanie[1] == 3 || zapytanie[1] == 4)
                    {
                        //kalkulcja odpowidzi
                        int countReq = BitConverter.ToInt16(new byte[2] { zapytanie[5], zapytanie[4] }, 0);
                        if (countReq * 2 != odpowiedz[2])
                            throw new ApplicationException("Response length isnt appropriate fct: 3, 4");

                        //Sprawdznie czy pole dlugosć odpowiedzi odzwierciedla stan faktyczny
                        if (odpowiedz.Length != odpowiedz[2] + 5)
                            throw new ApplicationException("Remote device problem. Response length isnt appropriate fct: 3, 4");
                    }

                    #endregion Read HR i DI

                    #region WriteSingleCoil and WriteSingleRegester

                    if (zapytanie[1] == 5 || zapytanie[1] == 6)
                    {
                        if (!Enumerable.SequenceEqual(zapytanie.ToList().GetRange(0, 6), odpowiedz.ToList().GetRange(0, 6)))
                            throw new ApplicationException("Response and Request isnt equals - (fct 5, 6).");
                    }

                    #endregion WriteSingleCoil and WriteSingleRegester

                    #region WriteMultipleCoil and WriteMultipleRegester

                    if (zapytanie[1] == 15 || zapytanie[1] == 16)
                    {
                        if (!Enumerable.SequenceEqual(zapytanie.ToList().GetRange(0, 6), odpowiedz.ToList().GetRange(0, 6)))
                            throw new ApplicationException("Response and Request isn't equals - (fct 15, 16).");
                    }

                    #endregion WriteMultipleCoil and WriteMultipleRegester
                }

                //Prawdopodobnie exeption
                else if (odpowiedz.Length == 5)
                {
                    //Jezeli CRC jest ok rzuć wyjatek
                    throw new ApplicationException(String.Format("Exception - Function: {0}    Code: {1}", odpowiedz[1], odpowiedz[2]));
                }
                else
                    //Niepoprawna dlugość ramki
                    throw new ApplicationException("Bad format frame, data length to low.");

                return true;
            }
            catch (Exception Ex)
            {
                if (sendInfoEv != null)
                    sendInfoEv(this, new ProjectEventArgs(odpowiedz, DateTime.Now, "ModbusMasterRTU.checkResponse :" + Ex.Message));

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
            switch (ask[1])
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

        /// <summary>
        /// Obliczenie ile danych jest potrzebnych
        /// </summary>
        /// <param name="frame"></param>
        /// <returns></returns>
        private int responseDataCount(byte[] frame)
        {
            #region Read CO i DI

            //Read DI and CO
            if (frame[1] == 1 || frame[1] == 2)
            {
                //DI CO
                int countReq = BitConverter.ToInt16(new byte[2] { frame[5], frame[4] }, 0);
                int reszta = (int)(float)(countReq % 8);
                int byteCount = countReq / 8;

                //Sprawdzenie czy jest reszta
                if (reszta > 0)
                    byteCount++;

                return byteCount + 5;
            }

            #endregion Read CO i DI

            #region Read HR i DI

            if (frame[1] == 3 || frame[1] == 4)
            {
                //kalkulcja odpowidzi
                int countReq = BitConverter.ToInt16(new byte[2] { frame[5], frame[4] }, 0);
                return 5 + (countReq * 2);
            }

            #endregion Read HR i DI

            #region Write sDI i sHR

            if (frame[1] == 5 || frame[1] == 6)
                return 8;

            #endregion Write sDI i sHR

            #region Write mDI i mHR

            if (frame[1] == 15 || frame[1] == 16)
                return 8;

            #endregion Write mDI i mHR

            return 500;
        }

        /// <summary>
        /// Klasa ustawiająca parametry komunkacujne
        /// </summary>

        /// <summary>
        /// Ustawienie klasy parametryzacji transmisji
        /// </summary>
        object IDriverModel.setDriverParam
        {
            get
            {
                return DriverParam_;
            }
            set
            {
                DriverParam_ = (IoDriverParam)value;
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
                String modbusHeader = String.Empty;
                String restData = string.Empty;

                switch (num)
                {
                    case NumberStyles.HexNumber:
                        if (frame[1] <= 4)
                        {
                            modbusHeader = String.Format("{0:X}   {1:X}   {2:X}{3:X}   {4:X}{5:X}   {6:X} {7:X}", frame[0], frame[1], frame[2], frame[3], frame[4], frame[5], frame[6], frame[7]);
                            return modbusHeader;
                        }

                        if (frame[1] == 5 || frame[1] == 6)
                        {
                            modbusHeader = String.Format("{0:X}   {1:X}   {2:X}{3:X}   {4:X} {5:X}   {6:X} {7:X}", frame[0], frame[1], frame[2], frame[3], frame[4], frame[5], frame[6], frame[7]);
                            return modbusHeader;
                        }
                        else
                        {
                            modbusHeader = String.Format("{0:X}   {1:X}   {2:X}{3:X}   {4:X}{5:X}   {6:X}", frame[0], frame[1], frame[2], frame[3], frame[4], frame[5], frame[6]);
                            restData = frame.ToList().GetRange(8, frame.Length - 10).Aggregate(String.Format("{0:X}", frame[7]), (sum, next) => sum + " " + String.Format("{0:X}", next));
                            return modbusHeader + "   " + restData + restData + String.Format("   {0:X} {1:X}", frame[frame.Length - 2], frame[frame.Length - 1]);
                        }

                    case NumberStyles.Integer:
                        if (frame[1] <= 4)
                        {
                            modbusHeader = String.Format("{0}   {1}   {2}{3}   {4}{5} {6} {7}", frame[0], frame[1], frame[2], frame[3], frame[4], frame[5], frame[6], frame[7]);
                            return modbusHeader;
                        }

                        if (frame[1] == 5 || frame[1] == 6)
                        {
                            modbusHeader = String.Format("{0}   {1}   {2}{3}   {4} {5}   {6} {7}", frame[0], frame[1], frame[2], frame[3], frame[4], frame[5], frame[6], frame[7]);
                            return modbusHeader;
                        }
                        else
                        {
                            modbusHeader = String.Format("{0}   {1}   {2}{3}   {4}{5}   {6}", frame[0], frame[1], frame[2], frame[3], frame[4], frame[5], frame[6]);
                            restData = frame.ToList().GetRange(8, frame.Length - 10).Aggregate(String.Format("{0}", frame[7]), (sum, next) => sum + " " + String.Format("{0}", next));
                            return modbusHeader + "   " + restData + String.Format("   {0} {1}", frame[frame.Length - 2], frame[frame.Length - 1]);
                        }

                    default:
                        if (frame[1] <= 4)
                        {
                            modbusHeader = String.Format("{0}   {1}   {2}{3}   {4}{5}   {6} {7}", frame[0], frame[1], frame[2], frame[3], frame[4], frame[5], frame[6], frame[7]);
                            return modbusHeader;
                        }

                        if (frame[1] == 5 || frame[1] == 6)
                        {
                            modbusHeader = String.Format("{0}   {1}   {2}{3}   {4} {5}   {6} {7}", frame[0], frame[1], frame[2], frame[3], frame[4], frame[5], frame[6], frame[7]);
                            return modbusHeader;
                        }
                        else
                        {
                            modbusHeader = String.Format("{0}   {1}   {2}{3}   {4}{5}   {6}", frame[0], frame[1], frame[2], frame[3], frame[4], frame[5], frame[6]);
                            restData = frame.ToList().GetRange(8, frame.Length - 10).Aggregate(String.Format("{0}", frame[7]), (sum, next) => sum + " " + String.Format("{0}", next));
                            return modbusHeader + "   " + restData + restData + String.Format("   {0} {1}", frame[frame.Length - 2], frame[frame.Length - 1]);
                        }
                }
            }
            catch (Exception Ex)
            {
                if (sendInfoEv != null)
                    sendInfoEv(this, new ProjectEventArgs(frame, DateTime.Now, "ModbusMasterRTU.FormatFrameRequest :" + Ex.Message));

                return "Fault: " + frame.Aggregate("", (sum, next) => sum + " " + next.ToString());
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
                String modbusHeader = String.Empty;
                String restData = String.Empty;

                switch (num)
                {
                    case NumberStyles.HexNumber:
                        //Read Area
                        if (frame[1] <= 4)
                        {
                            modbusHeader = String.Format("{0:X}   {1:X}   {2:X}", frame[0], frame[1], frame[2]);
                            restData = frame.ToList().GetRange(4, frame.Length - 6).Aggregate(String.Format("{0:X}", frame[3]), (sum, next) => sum + " " + String.Format("{0:X}", next));
                            return modbusHeader + "   " + restData + String.Format("   {0:X} {1:X}", frame[frame.Length - 2], frame[frame.Length - 1]); ;
                        }
                        //Single Cols and Single Register
                        else if (frame[1] == 5 || frame[1] == 6)
                        {
                            modbusHeader = String.Format("{0:X}   {1:X}   {2:X}{3:X}   {4:X} {5:X}   {6:X} {7:X}", frame[0], frame[1], frame[2], frame[3], frame[4], frame[5], frame[6], frame[7]);
                            return modbusHeader;
                        }
                        //Multiple Coils and Multiple Registers
                        else if (frame[1] == 15 || frame[1] == 16)
                        {
                            modbusHeader = String.Format("{0:X}   {1:X}   {2:X}{3:X}   {4:X}{5:X}   {6:X} {7:X}", frame[0], frame[1], frame[2], frame[3], frame[4], frame[5], frame[6], frame[7]);
                            return modbusHeader;
                        }
                        //Exceptions
                        else
                        {
                            modbusHeader = String.Format("{0:X}   {1:X}   {2:X}   {3:X}{4:X}", frame[0], frame[1], frame[2], frame[3], frame[4]);
                            return modbusHeader;
                        }

                    case NumberStyles.Integer:
                        //Read Area
                        if (frame[1] <= 4)
                        {
                            modbusHeader = String.Format("{0}   {1}   {2}", frame[0], frame[1], frame[2]);
                            restData = frame.ToList().GetRange(4, frame.Length - 6).Aggregate(String.Format("{0}", frame[3]), (sum, next) => sum + " " + String.Format("{0}", next));
                            return modbusHeader + "   " + restData + String.Format("   {0} {1}", frame[frame.Length - 2], frame[frame.Length - 1]); ;
                        }
                        //Single Cols and Single Register
                        else if (frame[1] == 5 || frame[1] == 6)
                        {
                            modbusHeader = String.Format("{0}   {1}   {2}{3}   {4} {5}   {6} {7}", frame[0], frame[1], frame[2], frame[3], frame[4], frame[5], frame[6], frame[7]);
                            return modbusHeader;
                        }
                        //Multiple Coils and Multiple Registers
                        else if (frame[1] == 15 || frame[1] == 16)
                        {
                            modbusHeader = String.Format("{0}   {1}   {2}{3}   {4}{5}   {6} {7}", frame[0], frame[1], frame[2], frame[3], frame[4], frame[5], frame[6], frame[7]);
                            return modbusHeader;
                        }
                        //Exceptions
                        else
                        {
                            modbusHeader = String.Format("{0}   {1}   {2}   {3}{4}", frame[0], frame[1], frame[2], frame[3], frame[4]);
                            return modbusHeader;
                        }

                    default:
                        //Read Area
                        if (frame[1] <= 4)
                        {
                            modbusHeader = String.Format("{0}   {1}   {2}", frame[0], frame[1], frame[2]);
                            restData = frame.ToList().GetRange(4, frame.Length - 6).Aggregate(String.Format("{0}", frame[3]), (sum, next) => sum + " " + String.Format("{0}", next));
                            return modbusHeader + "   " + restData + String.Format("   {0} {1}", frame[frame.Length - 2], frame[frame.Length - 1]); ;
                        }
                        //Single Cols and Single Register
                        else if (frame[1] == 5 || frame[1] == 6)
                        {
                            modbusHeader = String.Format("{0}   {1}   {2}{3}   {4} {5}   {6} {7}", frame[0], frame[1], frame[2], frame[3], frame[4], frame[5], frame[6], frame[7]);
                            return modbusHeader;
                        }
                        //Multiple Coils and Multiple Registers
                        else if (frame[1] == 15 || frame[1] == 16)
                        {
                            modbusHeader = String.Format("{0}   {1}   {2}{3}   {4}{5}   {6} {7}", frame[0], frame[1], frame[2], frame[3], frame[4], frame[5], frame[6], frame[7]);
                            return modbusHeader;
                        }
                        //Exceptions
                        else
                        {
                            modbusHeader = String.Format("{0}   {1}   {2}   {3}{4}", frame[0], frame[1], frame[2], frame[3], frame[4]);
                            return modbusHeader;
                        }
                }
            }
            catch (Exception Ex)
            {
                if (sendInfoEv != null)
                    sendInfoEv(this, new ProjectEventArgs(frame, DateTime.Now, "ModbusMasterRTU.FormatFrameResponse :" + Ex.Message));

                return "Fault: " + frame.Aggregate("", (sum, next) => sum + " " + next.ToString());
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
        /// Sterownik zajety
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
                    sPort.Dispose();
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
            Dispose(true);
        }

        #endregion IDisposable Support
    }
}