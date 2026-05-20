# LINE Messaging API and Face Scanning

โปรเจ็กต์นี้เป็นระบบตัวอย่างบน ASP.NET Web Forms สำหรับส่งข้อความและรูปภาพผ่าน LINE Messaging API ร่วมกับการสแกนใบหน้าแบบ realtime ด้วย `face-api.js` เพื่อให้สามารถตรวจจับใบหน้า, เทียบกับฐานข้อมูลใบหน้าที่ลงทะเบียนไว้, บันทึกภาพผู้ที่ตรวจพบ, และส่งการแจ้งเตือนพร้อมรูปภาพไปที่ LINE ได้อัตโนมัติ

## ความสามารถหลัก

- ส่งข้อความและรูปภาพผ่าน LINE Messaging API
- อัปโหลดรูปภาพจากไฟล์หรือระบุ URL รูปภาพโดยตรง
- บันทึกรูปลงเซิร์ฟเวอร์และสร้าง preview image อัตโนมัติ
- ใช้กล้องมือถือหรือ webcam เพื่อสแกนใบหน้าแบบ realtime
- แสดงกรอบใบหน้าและ landmark บนภาพจากกล้อง
- ลงทะเบียนใบหน้าที่รู้จักพร้อมบันทึก `face descriptor` 128 ค่า
- ส่ง descriptor จาก browser ไปที่ API ฝั่ง server เพื่อหา top match
- บันทึกประวัติการตรวจพบใบหน้าแยกตามบุคคล
- เมื่อพบใบหน้าที่ตรงกัน สามารถส่งข้อความพร้อมรูปภาพไปที่ LINE อัตโนมัติ
- บังคับใช้ `TLS 1.2` สำหรับการเรียก LINE API

## เทคโนโลยีที่ใช้

- ASP.NET Web Forms
- VB.NET
- .NET Framework 4.7.2
- `face-api.js`
- IIS Express / IIS
- JSON file storage สำหรับ face descriptors ใน `App_Data`

## โครงสร้างหลักของโปรเจ็กต์

- `Line_Notification/home.aspx`
  หน้าหลักของระบบ มีทั้งส่วนส่ง LINE, อัปโหลดรูป, Face Scanner, ลงทะเบียนใบหน้า, ประวัติการตรวจพบ, และรายการรูปที่บันทึกไว้

- `Line_Notification/home.aspx.vb`
  logic ฝั่ง server สำหรับรับ postback, บันทึกรูปภาพ, ลงทะเบียน descriptor, ส่ง LINE, และ bind ข้อมูลกลับไปแสดงบนหน้าเว็บ

- `Line_Notification/Scripts/face-scanner.js`
  logic ฝั่ง client สำหรับเปิดกล้อง, โหลดโมเดล `face-api.js`, ตรวจจับใบหน้า, สร้าง descriptor, เรียก API เทียบใบหน้า, และสั่ง auto-save เมื่อพบคนที่ตรงกัน

- `Line_Notification/FaceMatch.ashx`
  endpoint สำหรับรับคำขอเทียบใบหน้า

- `Line_Notification/FaceMatchHandler.vb`
  handler สำหรับรับ descriptor จาก browser และตอบผลการ match กลับเป็น JSON

- `Line_Notification/FaceDescriptorStore.vb`
  จัดการฐานข้อมูล descriptor ใน `App_Data/known-face-descriptors.json` รวมถึงสรุปจำนวนบุคคล, จำนวนรูป, การบันทึก descriptor, และการหา top match

- `Line_Notification/Global.asax.vb`
  ตั้งค่า route หน้าแรกและบังคับใช้ `TLS 1.2` สำหรับ outbound requests

- `Line_Notification/Uploads/LineImages`
  เก็บรูปทั่วไปที่อัปโหลดหรือบันทึกจากหน้าเว็บ

- `Line_Notification/Uploads/KnownFaces`
  เก็บรูปอ้างอิงของบุคคลที่ลงทะเบียนไว้

- `Line_Notification/Uploads/RecognizedFaces`
  เก็บรูปที่บันทึกอัตโนมัติเมื่อระบบตรวจพบใบหน้าที่ตรงกับฐานข้อมูล

## การทำงานของระบบ

### 1. ส่งข้อความและรูปภาพไปที่ LINE

ผู้ใช้สามารถกรอกข้อความ, ระบุ URL รูปภาพ, หรืออัปโหลดไฟล์ภาพผ่านหน้า `home.aspx` จากนั้นฝั่ง server จะสร้าง payload ของ LINE Messaging API และเรียก broadcast endpoint เพื่อส่ง `text` และ `image` ไปยัง LINE

### 2. ลงทะเบียนใบหน้า

เมื่อผู้ใช้จับภาพจากกล้องหรืออัปโหลดรูปสำหรับลงทะเบียน:

1. browser จะใช้ `face-api.js` ตรวจหาใบหน้าในรูป
2. ระบบจะสร้าง `face descriptor` ความยาว 128 ค่า
3. descriptor จะถูกส่งกลับไปฝั่ง server ผ่าน hidden field
4. server จะบันทึกรูปอ้างอิงของบุคคลลงใน `Uploads/KnownFaces`
5. server จะบันทึก descriptor ลงใน `App_Data/known-face-descriptors.json`

แนวทางนี้ทำให้ไม่ต้องอ่านรูปอ้างอิงทั้งหมดใหม่ทุกครั้งเมื่อเริ่มสแกน

### 3. สแกนใบหน้าแบบ realtime

เมื่อกดเริ่มสแกน:

1. `face-scanner.js` จะโหลดโมเดล `TinyFaceDetector`, `faceLandmark68TinyNet`, และ `faceRecognitionNet`
2. browser จะเปิดกล้องและตรวจจับใบหน้าจาก video stream
3. ทุกครั้งที่พบใบหน้า ระบบจะสร้าง descriptor ของใบหน้าปัจจุบัน
4. descriptor จะถูกส่งไปที่ `FaceMatch.ashx`
5. server จะอ่านฐาน descriptor ที่บันทึกไว้และคำนวณหา top match
6. ผลลัพธ์จะถูกส่งกลับมาให้ browser เพื่อนำไปแสดงชื่อและคะแนนความคล้าย

### 4. ตรวจพบคนที่ตรงกันแล้วทำอะไรต่อ

ถ้าระบบพบใบหน้าที่ตรงกับฐานข้อมูล:

1. browser จะรอให้พบใบหน้าเดิมต่อเนื่องหลายเฟรมเพื่อลด false trigger
2. ระบบจะ crop เฉพาะบริเวณใบหน้าและสร้าง snapshot
3. ส่ง snapshot และชื่อบุคคลกลับไปที่ server แบบ postback
4. server จะบันทึกรูปไว้ใน `Uploads/RecognizedFaces/<ชื่อบุคคล>/`
5. server จะพยายามสร้าง public image URL จาก `Public Base URL`
6. ถ้า URL เป็น `HTTPS` และ LINE เข้าถึงได้จากภายนอก ระบบจะส่งข้อความพร้อมรูปภาพไปที่ LINE อัตโนมัติ

## หลักการเปรียบเทียบใบหน้า

ระบบไม่ได้รู้จักตัวตนของบุคคลด้วยตัวเอง แต่ใช้หลักการเปรียบเทียบความใกล้เคียงของ `face descriptor`

- descriptor คือชุดตัวเลข 128 ค่า ที่สรุปลักษณะสำคัญของใบหน้า
- ตอนลงทะเบียน ระบบจะผูก descriptor เหล่านี้เข้ากับชื่อบุคคลที่ผู้ใช้กำหนด
- ตอนสแกนจริง ระบบจะสร้าง descriptor ของใบหน้าปัจจุบันแล้วส่งไปให้ server
- server จะคำนวณระยะห่างแบบ Euclidean distance ระหว่าง descriptor ปัจจุบันกับ descriptor ที่อยู่ในฐานข้อมูล
- ถ้าค่าระยะห่างต่ำกว่าค่า threshold ที่กำหนด จะถือว่าเป็นบุคคลนั้น

## วิธีตั้งค่า

### 1. ตั้งค่า LINE Channel Access Token

แก้ไขไฟล์ `Line_Notification/Web.config`

```xml
<appSettings>
  <add key="LineChannelAccessToken" value="YOUR_LINE_CHANNEL_ACCESS_TOKEN" />
</appSettings>
```

### 2. ตั้งค่า Public Base URL

ถ้าต้องการให้ LINE ดึงรูปภาพที่เซิร์ฟเวอร์บันทึกไว้ได้:

- ต้องกำหนด `Public Base URL` เป็นโดเมน `HTTPS`
- URL นี้ต้องเข้าถึงได้จากอินเทอร์เน็ตจริง
- ห้ามใช้ `localhost` หรือ URL ภายในเครือข่ายที่ LINE เข้าถึงไม่ได้

### 3. โมเดลของ face-api.js

โมเดลถูกเก็บไว้ใน:

- `Line_Notification/Scripts/face-api/models-v20260520`

และตั้งค่าให้ IIS เสิร์ฟไฟล์ `.bin` ได้แล้วใน `Web.config`

## วิธีรันโปรเจ็กต์

1. เปิด solution `Line_Notification.sln` ด้วย Visual Studio
2. restore NuGet packages หาก Visual Studio ยังไม่ได้ restore ให้อัตโนมัติ
3. ตั้งค่า `LineChannelAccessToken` ใน `Web.config`
4. รันโปรเจ็กต์ด้วย IIS Express หรือ IIS
5. เปิดหน้า root ของเว็บ ซึ่ง route จะชี้มาที่ `home.aspx`

## หมายเหตุสำคัญ

- กล้องของ browser จะทำงานได้ดีเมื่อเปิดผ่าน `HTTPS` หรือ `localhost`
- การส่งรูปภาพไปที่ LINE ต้องใช้ public `HTTPS` URL เท่านั้น
- โปรเจ็กต์นี้ใช้ไฟล์ JSON เป็นที่เก็บ descriptor หลัก ไม่ได้ใช้ vector database
- ถ้าฐานข้อมูลใบหน้ามีจำนวนมากในระดับหลักพัน ควรพิจารณาย้ายระบบ match ไปยังฐานข้อมูลหรือบริการที่เหมาะกับ vector search มากขึ้น
- ฝั่งแอปถูกตั้งค่าให้ใช้ `TLS 1.2` เพื่อหลีกเลี่ยงปัญหา `Could not create SSL/TLS secure channel` ตอนเรียก LINE API

## ภาพรวม flow แบบย่อ

1. ผู้ใช้ลงทะเบียนใบหน้าพร้อมชื่อ
2. browser สร้าง descriptor และบันทึกลง server
3. ผู้ใช้เปิดกล้องเพื่อสแกน
4. browser ตรวจจับใบหน้าและส่ง descriptor ปัจจุบันไป API
5. server หา top match จากฐานข้อมูล descriptor
6. ถ้าพบคนที่ตรงกัน ระบบจะบันทึกรูปผู้ที่ตรวจพบ
7. server ส่งข้อความและรูปภาพไปยัง LINE อัตโนมัติ

## Repository

- GitHub: [PTHPV1/LINE-Messaging-API-and-Face-Scanning](https://github.com/PTHPV1/LINE-Messaging-API-and-Face-Scanning)
