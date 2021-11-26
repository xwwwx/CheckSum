# CheckSum
**此工具可針對目標資料夾進行檔案完整性驗證，比對出新增、修改、刪除的檔案，並產出文字報告。**

站台原本使用FCIV進行檔案完整性驗證，但FCIV僅能比對出修改後的檔案，若站台被新增惡意檔案或檔案遭刪除，將無法及時發現。因此製作這個工具以監測檔案的新增、刪除、修改。

## VirusTotal ##
2021.11 新增檢查檔案功能，介接 VirusTotal API，檢驗檔案可信度。
> 因VirusTotal API介接資源有限，設置掃描檔案類型清單，僅掃描清單內的檔案，清單內容於appsettings.json內設定。

## Setting ##
相關設定存放在appsettings.json內，編輯該檔案內的設定以符合需求。
### 設定屬性介紹 ##
* ScanDir : 檢查的根路徑，程式將針對該路徑進行完整性驗證。
* OutPut : 每次檢查產生得紀錄檔及報告將產生於此路徑內。
* Except : 將不須檢查的檔案或資料夾名稱以字串陣列型態存放於此。
* VirusTotalConfig : VirusTotal介接相關設定。
  - Scan : 是否進行檔案檢查。
  - ApiKey :  VirusTotal Api Key。
  - ScanExtension : 需掃描檔案類型副檔名以字串陣列型態存放於此。
* SendMail : 是否寄發通知信。
* MailConfig : 通知信相關設定。
  - Host : Host Domain。
  - Recipients : 收件者以字串陣列型態存放於此。
  - Subject : 通知信主旨。
  - User : 寄件者帳號。
  - Password : 寄件者密碼。