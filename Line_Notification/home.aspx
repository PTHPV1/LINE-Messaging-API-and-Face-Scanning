<%@ Page Language="vb" AutoEventWireup="false" CodeBehind="home.aspx.vb" Inherits="Line_Notification.test" ResponseEncoding="utf-8" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <meta charset="utf-8" />
    <meta http-equiv="X-UA-Compatible" content="IE=edge" />
    <title>LINE Notification Test</title>
</head>
<body style="margin: 0; background-color: #f3f4f6;">
    <form id="form1" runat="server" enctype="multipart/form-data">
        <div style="max-width: 1080px; margin: 32px auto; padding: 24px; font-family: 'Segoe UI', Tahoma, sans-serif; line-height: 1.6; border: 1px solid #d6d9df; border-radius: 16px; background-color: #ffffff;">
            <h1 style="margin-top: 0; margin-bottom: 8px; color: #111827;">LINE Messaging API and Face Scanning</h1>
            <p style="margin-top: 0; margin-bottom: 24px; color: #4b5563;">
                หน้านี้รองรับการส่งข้อความ, ส่งรูปภาพ, สแกนหน้ากระดาษจากกล้องมือถือ, สแกนใบหน้าด้วย face-api.js, บันทึกรูปลงเซิร์ฟเวอร์ และสแกนดูรายการรูปที่บันทึกไว้ในหน้าเดียว
            </p>

            <div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(320px, 1fr)); gap: 20px;">
                <div>
                    <div style="margin-bottom: 16px;">
                        <label for="txtMessage" style="display: block; font-weight: 600; margin-bottom: 6px;">ข้อความ</label>
                        <asp:TextBox ID="txtMessage" runat="server" TextMode="MultiLine" Rows="4" Width="100%" placeholder="ใส่ข้อความที่ต้องการส่ง"></asp:TextBox>
                    </div>

                    <div style="margin-bottom: 16px;">
                        <label for="txtImageUrl" style="display: block; font-weight: 600; margin-bottom: 6px;">Original Image URL</label>
                        <asp:TextBox ID="txtImageUrl" runat="server" Width="100%" placeholder="https://example.com/images/original.jpg"></asp:TextBox>
                        <div style="font-size: 13px; color: #6b7280; margin-top: 4px;">ใส่ URL รูปโดยตรงได้ และใช้ปุ่มบันทึกเพื่อดึงรูปเข้ามาเก็บในระบบได้ด้วย</div>
                    </div>

                    <div style="margin-bottom: 16px;">
                        <label for="txtPreviewImageUrl" style="display: block; font-weight: 600; margin-bottom: 6px;">Preview Image URL</label>
                        <asp:TextBox ID="txtPreviewImageUrl" runat="server" Width="100%" placeholder="https://example.com/images/preview.jpg"></asp:TextBox>
                        <div style="font-size: 13px; color: #6b7280; margin-top: 4px;">ถ้าเว้นว่างไว้ ระบบจะใช้ Original Image URL เป็น Preview ให้</div>
                    </div>

                    <div style="margin-bottom: 16px;">
                        <label for="txtPublicBaseUrl" style="display: block; font-weight: 600; margin-bottom: 6px;">Public Base URL สำหรับ LINE</label>
                        <asp:TextBox ID="txtPublicBaseUrl" runat="server" Width="100%" placeholder="https://your-domain.com"></asp:TextBox>
                        <div style="font-size: 13px; color: #6b7280; margin-top: 4px;">ต้องเป็น HTTPS และ LINE เข้าถึงได้จากภายนอก เมื่อใช้รูปที่บันทึกบนเซิร์ฟเวอร์นี้</div>
                    </div>
                </div>

                <div>
                    <div style="margin-bottom: 16px; padding: 14px; border: 1px solid #d1d5db; border-radius: 12px; background-color: #f9fafb;">
                        <div style="font-weight: 600; margin-bottom: 6px;">อัปโหลดรูปภาพจากไฟล์</div>
                        <asp:FileUpload ID="fuImage" runat="server" />
                        <div style="font-size: 13px; color: #6b7280; margin-top: 6px;">รองรับ JPG / JPEG / PNG และจะสร้าง preview image ให้อัตโนมัติ</div>
                    </div>

                    <div style="margin-bottom: 16px; padding: 14px; border: 1px solid #bfdbfe; border-radius: 12px; background-color: #eff6ff;">
                        <div style="font-weight: 600; margin-bottom: 6px; color: #1d4ed8;">สแกนหน้ากระดาษ / ถ่ายจากกล้อง</div>
                        <asp:FileUpload ID="fuScanImage" runat="server" />
                        <div style="font-size: 13px; color: #1e40af; margin-top: 6px;">บนมือถือสามารถแตะแล้วเปิดกล้องเพื่อถ่ายเอกสารหรือหน้ากระดาษได้ทันที</div>
                    </div>

                    <div style="display: flex; flex-wrap: wrap; gap: 10px; margin-bottom: 16px;">
                        <asp:Button ID="btnSend" runat="server" Text="ส่งข้อความ / รูปภาพ" style="padding: 10px 18px; border: 0; border-radius: 8px; background-color: #06c755; color: #ffffff; font-size: 15px; cursor: pointer;" />
                        <asp:Button ID="btnSaveOnly" runat="server" Text="บันทึกรูปภาพ" style="padding: 10px 18px; border: 0; border-radius: 8px; background-color: #2563eb; color: #ffffff; font-size: 15px; cursor: pointer;" />
                        <asp:Button ID="btnRefreshSaved" runat="server" Text="สแกนรูปที่บันทึกไว้" style="padding: 10px 18px; border: 1px solid #cbd5e1; border-radius: 8px; background-color: #ffffff; color: #0f172a; font-size: 15px; cursor: pointer;" CausesValidation="false" />
                    </div>

                    <div style="font-size: 13px; color: #6b7280;">
                        ถ้ามีการเลือกไฟล์หรือรูปจากกล้อง ระบบจะใช้ไฟล์นั้นก่อนค่า URL ที่กรอกไว้
                    </div>
                </div>
            </div>

            <div style="margin-top: 28px; padding-top: 20px; border-top: 1px solid #e5e7eb;">
                <h2 style="margin-top: 0; margin-bottom: 8px; color: #111827;">Face Scanner</h2>
                <p style="margin-top: 0; margin-bottom: 16px; color: #6b7280;">
                    ใช้กล้องสแกนใบหน้าแบบ realtime ด้วย face-api.js, แสดงกรอบใบหน้าและ landmark, เทียบกับฐานใบหน้าที่ลงทะเบียนไว้, จับภาพ snapshot แล้วบันทึกเข้าเซิร์ฟเวอร์ได้ทันที
                </p>

                <div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(320px, 1fr)); gap: 20px;">
                    <div>
                        <div style="position: relative; min-height: 360px; border-radius: 14px; overflow: hidden; background: linear-gradient(135deg, #111827, #1f2937); border: 1px solid #cbd5e1;">
                            <video id="faceScannerVideo" autoplay="autoplay" muted="muted" playsinline="playsinline" style="display: block; width: 100%; height: 100%; min-height: 360px; object-fit: cover;"></video>
                            <canvas id="faceScannerOverlay" style="position: absolute; inset: 0; width: 100%; height: 100%;"></canvas>
                            <div id="faceScannerEmptyState" style="position: absolute; inset: 0; display: flex; align-items: center; justify-content: center; text-align: center; padding: 24px; color: #e5e7eb; background: rgba(17, 24, 39, 0.38);">
                                กด "เปิดกล้องสแกนใบหน้า" เพื่อเริ่มใช้งาน
                            </div>
                        </div>
                        <canvas id="faceSnapshotCanvas" style="display: none;"></canvas>
                    </div>

                    <div>
                        <div style="display: flex; flex-wrap: wrap; gap: 10px; margin-bottom: 16px;">
                            <button id="btnStartFaceScanner" type="button" style="padding: 10px 18px; border: 0; border-radius: 8px; background-color: #0f172a; color: #ffffff; font-size: 15px; cursor: pointer;">เปิดกล้องสแกนใบหน้า</button>
                            <button id="btnStopFaceScanner" type="button" style="padding: 10px 18px; border: 1px solid #cbd5e1; border-radius: 8px; background-color: #ffffff; color: #0f172a; font-size: 15px; cursor: pointer;">หยุดกล้อง</button>
                            <button id="btnCaptureFaceSnapshot" type="button" style="padding: 10px 18px; border: 0; border-radius: 8px; background-color: #f59e0b; color: #111827; font-size: 15px; cursor: pointer;">จับภาพใบหน้า</button>
                        </div>

                        <div id="faceScannerStatus" style="margin-bottom: 16px; padding: 12px 14px; border-radius: 10px; background-color: #eff6ff; color: #1d4ed8;">
                            ยังไม่ได้เริ่มสแกนใบหน้า
                        </div>

                        <div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(180px, 1fr)); gap: 12px; margin-bottom: 16px;">
                            <div style="padding: 14px; border: 1px solid #dbe4f0; border-radius: 12px; background-color: #f8fafc;">
                                <div style="font-size: 12px; color: #64748b;">จำนวนใบหน้า</div>
                                <div id="faceScannerFaceCount" style="font-size: 24px; font-weight: 700; color: #0f172a;">0</div>
                            </div>
                            <div style="padding: 14px; border: 1px solid #dbe4f0; border-radius: 12px; background-color: #f8fafc;">
                                <div style="font-size: 12px; color: #64748b;">ความมั่นใจสูงสุด</div>
                                <div id="faceScannerBestScore" style="font-size: 24px; font-weight: 700; color: #0f172a;">0%</div>
                            </div>
                            <div style="padding: 14px; border: 1px solid #dbe4f0; border-radius: 12px; background-color: #f8fafc;">
                                <div style="font-size: 12px; color: #64748b;">สถานะสแกน</div>
                                <div id="faceScannerMode" style="font-size: 18px; font-weight: 700; color: #0f172a;">พร้อมใช้งาน</div>
                            </div>
                            <div style="padding: 14px; border: 1px solid #dbe4f0; border-radius: 12px; background-color: #f8fafc;">
                                <div style="font-size: 12px; color: #64748b;">อัปเดตล่าสุด</div>
                                <div id="faceScannerLastUpdate" style="font-size: 18px; font-weight: 700; color: #0f172a;">-</div>
                            </div>
                            <div style="padding: 14px; border: 1px solid #dbe4f0; border-radius: 12px; background-color: #f8fafc;">
                                <div style="font-size: 12px; color: #64748b;">ฐานข้อมูลใบหน้า</div>
                                <div id="faceScannerKnownFaceCount" style="font-size: 24px; font-weight: 700; color: #0f172a;">0 คน</div>
                            </div>
                            <div style="padding: 14px; border: 1px solid #dbe4f0; border-radius: 12px; background-color: #f8fafc;">
                                <div style="font-size: 12px; color: #64748b;">บุคคลล่าสุด</div>
                                <div id="faceScannerRecognizedName" style="font-size: 18px; font-weight: 700; color: #0f172a;">-</div>
                            </div>
                            <div style="padding: 14px; border: 1px solid #dbe4f0; border-radius: 12px; background-color: #f8fafc;">
                                <div style="font-size: 12px; color: #64748b;">ความคล้ายล่าสุด</div>
                                <div id="faceScannerRecognizedScore" style="font-size: 24px; font-weight: 700; color: #0f172a;">-</div>
                            </div>
                        </div>

                        <div style="font-size: 13px; color: #6b7280; margin-bottom: 16px;">
                            แนะนำให้เปิดหน้านี้ผ่าน HTTPS หรือ `localhost` เพื่อให้ browser อนุญาตการใช้งานกล้อง
                        </div>

                        <asp:HiddenField ID="hfFaceSnapshotData" runat="server" />
                        <asp:HiddenField ID="hfKnownFacesCatalog" runat="server" />
                        <asp:HiddenField ID="hfKnownFaceDescriptorData" runat="server" />
                        <asp:HiddenField ID="hfRecognizedSnapshotData" runat="server" />
                        <asp:HiddenField ID="hfRecognizedPersonName" runat="server" />
                        <asp:HiddenField ID="hfResumeFaceScanner" runat="server" />
                        <asp:Button ID="btnSaveFaceSnapshot" runat="server" Text="บันทึกภาพสแกนใบหน้า" CausesValidation="false" style="padding: 10px 18px; border: 0; border-radius: 8px; background-color: #7c3aed; color: #ffffff; font-size: 15px; cursor: pointer;" />
                        <asp:Button ID="btnSaveRecognizedFace" runat="server" Text="บันทึกใบหน้าที่ตรวจพบอัตโนมัติ" CausesValidation="false" style="display: none;" />

                        <div style="margin-top: 12px; font-size: 13px; color: #0f766e;">
                            เมื่อระบบพบใบหน้าที่ตรงกับฐานข้อมูล จะจับภาพใบหน้า, บันทึกชื่อผู้ที่ตรวจพบ และส่งข้อความพร้อมรูปภาพไปที่ LINE ให้อัตโนมัติ
                        </div>

                        <div id="faceSnapshotPreviewWrap" style="display: none; margin-top: 16px; padding: 14px; border-radius: 12px; background-color: #faf5ff; border: 1px solid #e9d5ff;">
                            <div style="font-weight: 600; color: #581c87; margin-bottom: 8px;">ตัวอย่างภาพที่จับได้</div>
                            <img id="faceSnapshotPreview" alt="face snapshot preview" style="display: block; width: 100%; max-width: 320px; border-radius: 10px; border: 1px solid #ddd6fe;" />
                        </div>

                        <div style="margin-top: 16px; padding: 14px; border-radius: 12px; background-color: #f8fafc; border: 1px solid #dbe4f0;">
                            <div style="font-weight: 600; color: #111827; margin-bottom: 8px;">ลงทะเบียนใบหน้าเพื่อใช้เปรียบเทียบ</div>
                            <label for="txtKnownFaceName" style="display: block; font-weight: 600; margin-bottom: 6px;">ชื่อบุคคล</label>
                            <asp:TextBox ID="txtKnownFaceName" runat="server" Width="100%" placeholder="เช่น สมชาย ใจดี"></asp:TextBox>
                            <div style="font-size: 13px; color: #64748b; margin-top: 6px;">
                                จับภาพจาก face scanner หรืออัปโหลดรูปที่เห็นใบหน้าชัดเจน แล้วใส่ชื่อก่อนกดลงทะเบียน
                            </div>
                            <div style="display: flex; flex-wrap: wrap; gap: 10px; margin-top: 12px;">
                                <asp:Button ID="btnRegisterKnownFace" runat="server" Text="ลงทะเบียนใบหน้า" CausesValidation="false" style="padding: 10px 18px; border: 0; border-radius: 8px; background-color: #0ea5e9; color: #ffffff; font-size: 15px; cursor: pointer;" />
                                <asp:Button ID="btnRefreshKnownFaces" runat="server" Text="รีเฟรชฐานข้อมูลใบหน้า" CausesValidation="false" style="padding: 10px 18px; border: 1px solid #cbd5e1; border-radius: 8px; background-color: #ffffff; color: #0f172a; font-size: 15px; cursor: pointer;" />
                            </div>
                        </div>
                    </div>
                </div>
            </div>

            <asp:Literal ID="litStatus" runat="server"></asp:Literal>
            <asp:Literal ID="litImageInfo" runat="server"></asp:Literal>

            <div style="margin-top: 28px; padding-top: 20px; border-top: 1px solid #e5e7eb;">
                <h2 style="margin-top: 0; margin-bottom: 8px; color: #111827;">ฐานข้อมูลใบหน้าที่รู้จัก</h2>
                <p style="margin-top: 0; margin-bottom: 16px; color: #6b7280;">
                    ระบบจะใช้รูปในส่วนนี้เพื่อเทียบว่าคนที่สแกนอยู่คือใคร
                </p>
                <asp:Literal ID="litKnownFaces" runat="server"></asp:Literal>
            </div>

            <div style="margin-top: 28px; padding-top: 20px; border-top: 1px solid #e5e7eb;">
                <h2 style="margin-top: 0; margin-bottom: 8px; color: #111827;">ประวัติการตรวจพบใบหน้า</h2>
                <p style="margin-top: 0; margin-bottom: 16px; color: #6b7280;">
                    รูปในส่วนนี้ถูกบันทึกอัตโนมัติเมื่อ face scanner พบคนที่ตรงกับฐานข้อมูล
                </p>
                <asp:Literal ID="litRecognizedFaces" runat="server"></asp:Literal>
            </div>

            <div style="margin-top: 28px; padding-top: 20px; border-top: 1px solid #e5e7eb;">
                <h2 style="margin-top: 0; margin-bottom: 8px; color: #111827;">รูปภาพที่บันทึกไว้</h2>
                <p style="margin-top: 0; margin-bottom: 16px; color: #6b7280;">
                    ส่วนนี้จะแสกนโฟลเดอร์ `Uploads/LineImages` แล้วแสดงรูปที่บันทึกล่าสุด
                </p>
                <asp:Literal ID="litSavedImages" runat="server"></asp:Literal>
            </div>
        </div>
    </form>

    <script type="text/javascript">
        window.faceScannerConfig = {
            modelPath: '<%= ResolveUrl("~/Scripts/face-api/models-v20260520") %>',
            faceMatchApiUrl: '<%= ResolveUrl("~/FaceMatch.ashx") %>',
            matchRequestIntervalMs: 400,
            snapshotFieldId: '<%= hfFaceSnapshotData.ClientID %>',
            saveButtonId: '<%= btnSaveFaceSnapshot.ClientID %>',
            knownFacesFieldId: '<%= hfKnownFacesCatalog.ClientID %>',
            knownFaceDescriptorFieldId: '<%= hfKnownFaceDescriptorData.ClientID %>',
            recognizedSnapshotFieldId: '<%= hfRecognizedSnapshotData.ClientID %>',
            recognizedNameFieldId: '<%= hfRecognizedPersonName.ClientID %>',
            recognizedSaveButtonId: '<%= btnSaveRecognizedFace.ClientID %>',
            resumeFieldId: '<%= hfResumeFaceScanner.ClientID %>',
            registerButtonId: '<%= btnRegisterKnownFace.ClientID %>',
            uploadInputId: '<%= fuImage.ClientID %>',
            scanUploadInputId: '<%= fuScanImage.ClientID %>',
            imageUrlInputId: '<%= txtImageUrl.ClientID %>'
        };
    </script>
    <script type="text/javascript" src="<%= ResolveUrl("~/Scripts/face-api/face-api.min.js") %>"></script>
    <script type="text/javascript" src="<%= ResolveUrl("~/Scripts/face-scanner.js?v=20260520-face-match-api") %>"></script>
</body>
</html>
