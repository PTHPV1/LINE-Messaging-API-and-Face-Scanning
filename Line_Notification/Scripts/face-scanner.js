(function () {
  'use strict';

  function onReady(callback) {
    if (document.readyState === 'loading') {
      document.addEventListener('DOMContentLoaded', callback);
      return;
    }

    callback();
  }

  onReady(function () {
    var config = window.faceScannerConfig || {};
    var video = document.getElementById('faceScannerVideo');
    var overlay = document.getElementById('faceScannerOverlay');
    var emptyState = document.getElementById('faceScannerEmptyState');
    var statusBox = document.getElementById('faceScannerStatus');
    var faceCountText = document.getElementById('faceScannerFaceCount');
    var bestScoreText = document.getElementById('faceScannerBestScore');
    var modeText = document.getElementById('faceScannerMode');
    var lastUpdateText = document.getElementById('faceScannerLastUpdate');
    var knownFaceCountText = document.getElementById('faceScannerKnownFaceCount');
    var recognizedNameText = document.getElementById('faceScannerRecognizedName');
    var recognizedScoreText = document.getElementById('faceScannerRecognizedScore');
    var startButton = document.getElementById('btnStartFaceScanner');
    var stopButton = document.getElementById('btnStopFaceScanner');
    var captureButton = document.getElementById('btnCaptureFaceSnapshot');
    var snapshotCanvas = document.getElementById('faceSnapshotCanvas');
    var snapshotPreview = document.getElementById('faceSnapshotPreview');
    var snapshotPreviewWrap = document.getElementById('faceSnapshotPreviewWrap');
    var snapshotField = document.getElementById(config.snapshotFieldId);
    var saveButton = document.getElementById(config.saveButtonId);
    var knownFacesField = document.getElementById(config.knownFacesFieldId);
    var knownFaceDescriptorField = document.getElementById(config.knownFaceDescriptorFieldId);
    var recognizedSnapshotField = document.getElementById(config.recognizedSnapshotFieldId);
    var recognizedNameField = document.getElementById(config.recognizedNameFieldId);
    var recognizedSaveButton = document.getElementById(config.recognizedSaveButtonId);
    var resumeField = document.getElementById(config.resumeFieldId);
    var registerButton = document.getElementById(config.registerButtonId);
    var uploadInput = document.getElementById(config.uploadInputId);
    var scanUploadInput = document.getElementById(config.scanUploadInputId);
    var imageUrlInput = document.getElementById(config.imageUrlInputId);

    if (
      !video ||
      !overlay ||
      !emptyState ||
      !statusBox ||
      !faceCountText ||
      !bestScoreText ||
      !modeText ||
      !lastUpdateText ||
      !startButton ||
      !stopButton ||
      !captureButton ||
      !snapshotCanvas ||
      !snapshotPreview ||
      !snapshotPreviewWrap ||
      !snapshotField ||
      !saveButton ||
      !knownFaceDescriptorField ||
      !recognizedSnapshotField ||
      !recognizedNameField ||
      !recognizedSaveButton ||
      !resumeField ||
      !registerButton ||
      !uploadInput ||
      !scanUploadInput ||
      !imageUrlInput
    ) {
      return;
    }

    var messages = {
      browserNeedsPermission: 'Browser ไม่อนุญาตให้ใช้กล้อง กรุณาอนุญาตการเข้าถึงกล้องแล้วลองใหม่',
      cameraNotFound: 'ไม่พบกล้องบนอุปกรณ์นี้',
      cameraBusy: 'กล้องถูกใช้งานอยู่หรือยังไม่พร้อม กรุณาปิดแอปอื่นที่ใช้กล้องแล้วลองใหม่',
      cameraConstraintFailed: 'ไม่สามารถเปิดกล้องด้วยค่าความละเอียดที่ต้องการได้ กรุณาลองใหม่อีกครั้ง',
      modelShapeError: 'ไม่สามารถโหลดโมเดล face scanner ได้ครบ กรุณารีเฟรชหน้าแล้วลองใหม่',
      modelFetchError: 'ไม่สามารถโหลดไฟล์โมเดลของ face scanner ได้ กรุณาตรวจสอบว่าไฟล์โมเดลอยู่ครบ',
      browserNotSupported: 'Browser นี้ไม่รองรับการเข้าถึงกล้อง',
      secureContextRequired: 'การใช้กล้องต้องเปิดผ่าน HTTPS หรือ localhost',
      faceApiMissing: 'ไม่พบ face-api.js ในหน้าเว็บ',
      loadingModels: 'กำลังโหลดโมเดล face-api.js ...',
      modelsReady: 'โหลดโมเดลสำเร็จ พร้อมเริ่มสแกนใบหน้า',
      loadingKnownFaces: 'กำลังเตรียมฐานข้อมูลใบหน้า ...',
      loadingKnownFaceDb: 'กำลังโหลดฐานข้อมูล',
      readyToMatch: 'พร้อมเปรียบเทียบ',
      noComparableFace: 'ไม่มีรูปที่ใช้เทียบได้',
      scannerAlreadyRunning: 'กล้องกำลังทำงานอยู่แล้ว',
      cameraAndMatchRunning: 'กำลังสแกนและเปรียบเทียบใบหน้าแบบ realtime',
      cameraRunning: 'กำลังสแกนใบหน้าแบบ realtime',
      scannerRunning: 'กำลังสแกน',
      openCameraFailed: 'ไม่สามารถเปิดกล้องหรือโหลดโมเดลได้',
      scannerStoppedMode: 'หยุดสแกน',
      scannerStopped: 'หยุดกล้องสแกนใบหน้าแล้ว',
      noFaceDb: 'ยังไม่มีฐานข้อมูล',
      noFaceFoundYet: 'ยังไม่พบใบหน้า',
      unknownFace: 'ไม่รู้จัก',
      ready: 'พร้อมใช้งาน',
      noFaceInFrame: 'ไม่พบใบหน้า',
      noFaceInCurrentFrame: 'ไม่พบใบหน้าในเฟรมปัจจุบัน',
      detectedOneFace: 'พบ 1 ใบหน้า',
      detectedManyFaces: 'พบหลายใบหน้า',
      faceDetectedButUnknown: 'ตรวจพบใบหน้า แต่ยังไม่ตรงกับฐานข้อมูล',
      matchApiFailed: 'ไม่สามารถเรียก API เทียบใบหน้าได้',
      matchingLabel: 'กำลังเทียบ',
      matchingOnServer: 'กำลังเปรียบเทียบกับฐานข้อมูล ...',
      oneFaceDetected: 'ตรวจพบใบหน้า 1 คน',
      manyFacesDetected: 'ตรวจพบหลายใบหน้าในเฟรมเดียวกัน',
      scanFailed: 'เกิดข้อผิดพลาดระหว่างสแกนใบหน้า',
      registerFaceLoading: 'กำลังสร้างตัวเลขอ้างอิงใบหน้าสำหรับลงทะเบียน ...',
      registerFaceMissing: 'ไม่พบภาพใบหน้าสำหรับใช้ลงทะเบียน',
      registerFaceNotFound: 'ไม่พบใบหน้าชัดเจนในรูปที่ใช้ลงทะเบียน',
      registerFaceReady: 'สร้างตัวเลขอ้างอิงใบหน้าเรียบร้อย กำลังบันทึกข้อมูล ...',
      registerFaceFailed: 'ไม่สามารถสร้างตัวเลขอ้างอิงใบหน้าสำหรับลงทะเบียนได้',
      captureOpenCameraFirst: 'กรุณาเปิดกล้องก่อนจับภาพใบหน้า',
      captureReady: 'จับภาพใบหน้าเรียบร้อยแล้ว',
      captureNoFace: 'จับภาพได้แล้ว แต่ยังไม่พบใบหน้าในเฟรมนี้',
      autoSaveInProgress: 'ตรวจพบใบหน้าที่ตรงกัน กำลังบันทึกภาพของ ',
      autoSaveFallbackError: 'ไม่สามารถบันทึกใบหน้าที่ตรวจพบอัตโนมัติได้',
      saveSnapshotFirst: 'กรุณาจับภาพจาก face scanner ก่อนบันทึก',
      notStarted: 'ยังไม่ได้เริ่มสแกนใบหน้า'
    };

    var state = {
      autoSaveInProgress: false,
      detectionTimer: 0,
      knownFaceSummary: null,
      lastMatches: [],
      lastMatchRequestAt: 0,
      lastMatchRequestSequence: 0,
      lastResults: [],
      loadedKnownFaceCount: 0,
      loadedKnownFaceSampleCount: 0,
      matchApiInFlightToken: 0,
      modelsLoaded: false,
      pendingRecognizedLabel: '',
      pendingRecognizedMatchIndex: -1,
      pendingRecognizedFrameCount: 0,
      registerDescriptorBusy: false,
      registerPostbackReady: false,
      running: false,
      scanSessionId: 0,
      statusKey: '',
      stream: null
    };

    var detectorOptions = null;
    var autoSaveCooldownMs = 15000;
    var autoSaveStableFramesRequired = 3;
    var autoSaveStorageKeyPrefix = 'line-notification-face-auto-save:';
    var matchRequestIntervalMs = Math.max(250, Number(config.matchRequestIntervalMs) || 400);
    var recognitionThreshold = 0.5;

    function setSaveButtonEnabled(isEnabled) {
      saveButton.disabled = !isEnabled;
      saveButton.style.opacity = isEnabled ? '1' : '0.55';
      saveButton.style.cursor = isEnabled ? 'pointer' : 'not-allowed';
    }

    function setStatus(message, kind) {
      var palette = {
        error: {
          background: '#fef2f2',
          color: '#b91c1c'
        },
        info: {
          background: '#eff6ff',
          color: '#1d4ed8'
        },
        success: {
          background: '#ecfdf5',
          color: '#166534'
        },
        warning: {
          background: '#fff7ed',
          color: '#c2410c'
        }
      };
      var selectedPalette = palette[kind] || palette.info;
      var statusKey = kind + ':' + message;

      if (state.statusKey === statusKey) {
        return;
      }

      state.statusKey = statusKey;
      statusBox.textContent = message;
      statusBox.style.backgroundColor = selectedPalette.background;
      statusBox.style.color = selectedPalette.color;
    }

    function getFriendlyErrorMessage(error, fallbackMessage) {
      var errorMessage = error && error.message ? String(error.message) : '';
      var errorName = error && error.name ? String(error.name) : '';
      var normalized = (errorName + ' ' + errorMessage).toLowerCase();

      if (
        normalized.indexOf('permission denied') >= 0 ||
        normalized.indexOf('notallowederror') >= 0 ||
        normalized.indexOf('permissiondismissederror') >= 0
      ) {
        return messages.browserNeedsPermission;
      }

      if (normalized.indexOf('notfounderror') >= 0 || normalized.indexOf('devicesnotfounderror') >= 0) {
        return messages.cameraNotFound;
      }

      if (normalized.indexOf('notreadableerror') >= 0 || normalized.indexOf('trackstarterror') >= 0) {
        return messages.cameraBusy;
      }

      if (
        normalized.indexOf('overconstrainederror') >= 0 ||
        normalized.indexOf('constraintnotsatisfiederror') >= 0
      ) {
        return messages.cameraConstraintFailed;
      }

      if (
        normalized.indexOf('tensor should have') >= 0 ||
        normalized.indexOf('shape') >= 0 ||
        normalized.indexOf('weights') >= 0
      ) {
        return messages.modelShapeError;
      }

      if (
        normalized.indexOf('failed to fetch') >= 0 ||
        normalized.indexOf('404') >= 0 ||
        normalized.indexOf('networkerror') >= 0
      ) {
        return messages.modelFetchError;
      }

      return errorMessage || fallbackMessage;
    }

    function setLastUpdateText() {
      var now = new Date();

      lastUpdateText.textContent = now.toLocaleTimeString([], {
        hour: '2-digit',
        minute: '2-digit',
        second: '2-digit'
      });
    }

    function updateStats(results, modeLabel) {
      var faces = Array.isArray(results) ? results : [];
      var bestScore = 0;

      faces.forEach(function (result) {
        if (result && result.detection && typeof result.detection.score === 'number' && result.detection.score > bestScore) {
          bestScore = result.detection.score;
        }
      });

      faceCountText.textContent = String(faces.length);
      bestScoreText.textContent = Math.round(bestScore * 100) + '%';
      modeText.textContent = modeLabel || messages.ready;
      setLastUpdateText();
    }

    function clearOverlay() {
      var context = overlay.getContext('2d');
      context.clearRect(0, 0, overlay.width, overlay.height);
    }

    function ensureOverlaySize() {
      var width = video.videoWidth || video.clientWidth;
      var height = video.videoHeight || video.clientHeight;

      if (!width || !height) {
        return null;
      }

      if (overlay.width !== width || overlay.height !== height) {
        overlay.width = width;
        overlay.height = height;
      }

      return {
        height: height,
        width: width
      };
    }

    function drawLabel(context, x, y, text) {
      context.font = '14px Segoe UI, sans-serif';
      var textWidth = context.measureText(text).width;
      var labelHeight = 24;
      var labelX = Math.max(0, x);
      var labelY = Math.max(labelHeight, y);

      context.fillStyle = 'rgba(15, 23, 42, 0.78)';
      context.fillRect(labelX, labelY - labelHeight, textWidth + 16, labelHeight);
      context.fillStyle = '#ffffff';
      context.fillText(text, labelX + 8, labelY - 8);
    }

    function distanceToPercent(distance) {
      if (typeof distance !== 'number' || !isFinite(distance)) {
        return 0;
      }

      return Math.max(0, Math.min(100, Math.round((1 - distance) * 100)));
    }

    function buildRecognitionLabel(match, index) {
      if (!match) {
        return 'Face ' + (index + 1);
      }

      if (match.pending) {
        return 'Face ' + (index + 1) + ' ' + messages.matchingLabel;
      }

      if (match.label === 'unknown') {
        return 'Face ' + (index + 1) + ' ' + messages.unknownFace;
      }

      return match.label + ' ' + distanceToPercent(match.distance) + '%';
    }

    function drawResults(results, displaySize, matches) {
      var context = overlay.getContext('2d');
      context.clearRect(0, 0, overlay.width, overlay.height);

      if (!results.length) {
        return;
      }

      var resizedResults = window.faceapi.resizeResults(results, displaySize);
      var resolvedMatches = Array.isArray(matches) ? matches : [];

      resizedResults.forEach(function (result, index) {
        var box = result.detection.box;
        var score = Math.round(result.detection.score * 100);
        var match = resolvedMatches[index] || null;
        var label = buildRecognitionLabel(match, index);

        context.strokeStyle = match && match.label !== 'unknown' ? '#0ea5e9' : '#06c755';
        context.lineWidth = 2.5;
        context.strokeRect(box.x, box.y, box.width, box.height);
        drawLabel(context, box.x, box.y, label + ' / ' + score + '%');

        if (result.landmarks && result.landmarks.positions) {
          context.fillStyle = '#60a5fa';

          result.landmarks.positions.forEach(function (point) {
            context.beginPath();
            context.arc(point.x, point.y, 1.6, 0, Math.PI * 2);
            context.fill();
          });
        }
      });
    }

    function countUniqueLabels(entries) {
      var seen = {};
      var uniqueCount = 0;

      (entries || []).forEach(function (entry) {
        if (!entry || !entry.label || seen[entry.label]) {
          return;
        }

        seen[entry.label] = true;
        uniqueCount += 1;
      });

      return uniqueCount;
    }

    function parseKnownFaceCatalogSummary() {
      if (!knownFacesField || !knownFacesField.value) {
        return {
          personCount: 0,
          sampleCount: 0
        };
      }

      try {
        var parsed = JSON.parse(knownFacesField.value);

        if (Array.isArray(parsed)) {
          return {
            personCount: countUniqueLabels(parsed),
            sampleCount: parsed.length
          };
        }

        return {
          personCount: Math.max(0, Number(parsed.personCount || parsed.PersonCount || 0)),
          sampleCount: Math.max(0, Number(parsed.sampleCount || parsed.SampleCount || 0))
        };
      } catch (error) {
        return {
          personCount: 0,
          sampleCount: 0
        };
      }
    }

    function refreshKnownFaceCatalogSummary() {
      state.knownFaceSummary = parseKnownFaceCatalogSummary();
      state.loadedKnownFaceCount = Math.max(0, Number(state.knownFaceSummary.personCount || 0));
      state.loadedKnownFaceSampleCount = Math.max(0, Number(state.knownFaceSummary.sampleCount || 0));
      updateKnownFaceSummary();
    }

    function hasKnownFaceDatabase() {
      return Math.max(0, Number(state.loadedKnownFaceSampleCount || 0)) > 0;
    }

    function buildPendingMatches(faceCount) {
      var pendingMatches = [];

      for (var index = 0; index < faceCount; index += 1) {
        pendingMatches.push({
          distance: 1,
          label: '',
          pending: true
        });
      }

      return pendingMatches;
    }

    function buildUnknownMatches(faceCount) {
      var unknownMatches = [];

      for (var index = 0; index < faceCount; index += 1) {
        unknownMatches.push({
          distance: 1,
          label: 'unknown',
          pending: false
        });
      }

      return unknownMatches;
    }

    function normalizeServerMatch(match) {
      var distance = match
        ? Number(match.distance !== undefined ? match.distance : match.Distance)
        : 1;
      var isMatch = match
        ? Boolean(match.isMatch !== undefined ? match.isMatch : match.IsMatch)
        : false;
      var label = match ? String(match.label || match.Label || '') : '';

      return {
        distance: isFinite(distance) ? distance : 1,
        fileName: match ? String(match.fileName || match.FileName || '') : '',
        label: isMatch && label ? label : 'unknown',
        pending: false
      };
    }

    function descriptorToArray(descriptor) {
      if (!descriptor || typeof descriptor.length !== 'number') {
        return [];
      }

      var descriptorValues = [];

      for (var index = 0; index < descriptor.length; index += 1) {
        descriptorValues.push(Number(descriptor[index]));
      }

      return descriptorValues.filter(function (value) {
        return isFinite(value);
      });
    }

    function setRecognitionSummary(label, scoreText) {
      if (recognizedNameText) {
        recognizedNameText.textContent = label || '-';
      }

      if (recognizedScoreText) {
        recognizedScoreText.textContent = scoreText || '-';
      }
    }

    function resetPendingRecognizedMatch() {
      state.pendingRecognizedLabel = '';
      state.pendingRecognizedMatchIndex = -1;
      state.pendingRecognizedFrameCount = 0;
    }

    function readStorageValue(storageKey) {
      try {
        return window.localStorage ? window.localStorage.getItem(storageKey) : null;
      } catch (error) {
        return null;
      }
    }

    function writeStorageValue(storageKey, storageValue) {
      try {
        if (window.localStorage) {
          window.localStorage.setItem(storageKey, storageValue);
        }
      } catch (error) {
        return;
      }
    }

    function getAutoSaveStorageKey(label) {
      return autoSaveStorageKeyPrefix + String(label || '').trim().toLowerCase();
    }

    function wasAutoSavedRecently(label) {
      if (!label) {
        return false;
      }

      var lastSavedAtText = readStorageValue(getAutoSaveStorageKey(label));
      var lastSavedAt = lastSavedAtText ? parseInt(lastSavedAtText, 10) : 0;

      if (!lastSavedAt || !isFinite(lastSavedAt)) {
        return false;
      }

      return Date.now() - lastSavedAt < autoSaveCooldownMs;
    }

    function markAutoSaved(label) {
      if (!label) {
        return;
      }

      writeStorageValue(getAutoSaveStorageKey(label), String(Date.now()));
    }

    function buildSnapshotDataUrl(sourceRect) {
      var videoWidth = video.videoWidth;
      var videoHeight = video.videoHeight;

      if (!videoWidth || !videoHeight) {
        return '';
      }

      var cropX = 0;
      var cropY = 0;
      var cropWidth = videoWidth;
      var cropHeight = videoHeight;

      if (sourceRect) {
        cropX = Math.max(0, Math.floor(sourceRect.x));
        cropY = Math.max(0, Math.floor(sourceRect.y));
        cropWidth = Math.max(1, Math.floor(sourceRect.width));
        cropHeight = Math.max(1, Math.floor(sourceRect.height));

        if (cropX + cropWidth > videoWidth) {
          cropWidth = videoWidth - cropX;
        }

        if (cropY + cropHeight > videoHeight) {
          cropHeight = videoHeight - cropY;
        }
      }

      snapshotCanvas.width = cropWidth;
      snapshotCanvas.height = cropHeight;

      var context = snapshotCanvas.getContext('2d');
      context.clearRect(0, 0, cropWidth, cropHeight);
      context.drawImage(video, cropX, cropY, cropWidth, cropHeight, 0, 0, cropWidth, cropHeight);

      return snapshotCanvas.toDataURL('image/jpeg', 0.92);
    }

    function buildDetectedFaceSnapshotDataUrl(result) {
      if (!result || !result.detection || !result.detection.box) {
        return buildSnapshotDataUrl(null);
      }

      var box = result.detection.box;
      var paddingX = box.width * 0.35;
      var paddingY = box.height * 0.45;

      return buildSnapshotDataUrl({
        height: box.height + paddingY * 2,
        width: box.width + paddingX * 2,
        x: box.x - paddingX,
        y: box.y - paddingY
      });
    }

    function triggerRecognizedFaceSave(label, result) {
      var snapshotDataUrl = buildDetectedFaceSnapshotDataUrl(result);

      if (!snapshotDataUrl) {
        setStatus(messages.autoSaveFallbackError, 'error');
        return;
      }

      state.autoSaveInProgress = true;
      markAutoSaved(label);
      recognizedSnapshotField.value = snapshotDataUrl;
      recognizedNameField.value = label;
      resumeField.value = '1';
      snapshotPreview.src = snapshotDataUrl;
      snapshotPreviewWrap.style.display = 'block';
      setStatus(messages.autoSaveInProgress + label, 'success');

      if (recognizedSaveButton && typeof recognizedSaveButton.click === 'function') {
        recognizedSaveButton.click();
      } else if (typeof window.__doPostBack === 'function' && recognizedSaveButton.name) {
        window.__doPostBack(recognizedSaveButton.name, '');
      } else {
        state.autoSaveInProgress = false;
        setStatus(messages.autoSaveFallbackError, 'error');
      }
    }

    function handleRecognizedAutoSave(bestRecognizedMatch) {
      if (!bestRecognizedMatch || !bestRecognizedMatch.match || !bestRecognizedMatch.result) {
        resetPendingRecognizedMatch();
        return;
      }

      var label = bestRecognizedMatch.match.label;

      if (!label || label === 'unknown') {
        resetPendingRecognizedMatch();
        return;
      }

      if (state.pendingRecognizedLabel === label) {
        state.pendingRecognizedFrameCount += 1;
        state.pendingRecognizedMatchIndex = bestRecognizedMatch.index;
      } else {
        state.pendingRecognizedLabel = label;
        state.pendingRecognizedMatchIndex = bestRecognizedMatch.index;
        state.pendingRecognizedFrameCount = 1;
      }

      if (state.autoSaveInProgress) {
        return;
      }

      if (state.pendingRecognizedFrameCount < autoSaveStableFramesRequired) {
        return;
      }

      if (wasAutoSavedRecently(label)) {
        return;
      }

      triggerRecognizedFaceSave(label, bestRecognizedMatch.result);
    }

    function updateKnownFaceSummary() {
      if (!knownFaceCountText) {
        return;
      }

      var personCount = Math.max(
        0,
        Number(
          state.loadedKnownFaceCount ||
            (state.knownFaceSummary && (state.knownFaceSummary.personCount || state.knownFaceSummary.PersonCount)) ||
            0
        )
      );
      var sampleCount = Math.max(
        0,
        Number(
          state.loadedKnownFaceSampleCount ||
            (state.knownFaceSummary && (state.knownFaceSummary.sampleCount || state.knownFaceSummary.SampleCount)) ||
            0
        )
      );

      if (!personCount) {
        knownFaceCountText.textContent = '0 คน';
        return;
      }

      knownFaceCountText.textContent = personCount + ' คน / ' + sampleCount + ' รูป';
    }

    function updateRecognitionSummary(matches) {
      if (!hasKnownFaceDatabase()) {
        setRecognitionSummary(messages.noFaceDb, '-');
        return;
      }

      if (!state.running) {
        setRecognitionSummary(messages.readyToMatch, '-');
        return;
      }

      if (!state.lastResults.length) {
        setRecognitionSummary(messages.noFaceFoundYet, '-');
        return;
      }

      if (!Array.isArray(matches) || !matches.length) {
        setRecognitionSummary(messages.matchingOnServer, '-');
        return;
      }

      var resolvedMatches = matches.filter(function (match) {
        return match && !match.pending;
      });

      if (!resolvedMatches.length) {
        setRecognitionSummary(messages.matchingOnServer, '-');
        return;
      }

      var recognizedMatches = resolvedMatches.filter(function (match) {
        return match && match.label && match.label !== 'unknown';
      });

      if (!recognizedMatches.length) {
        setRecognitionSummary(messages.unknownFace, '-');
        return;
      }

      var bestMatch = recognizedMatches.reduce(function (best, current) {
        return current.distance < best.distance ? current : best;
      });

      setRecognitionSummary(bestMatch.label, distanceToPercent(bestMatch.distance) + '%');
    }

    function loadImageElement(url) {
      return new Promise(function (resolve, reject) {
        var image = new Image();
        if (/^https?:/i.test(url)) {
          image.crossOrigin = 'anonymous';
        }
        image.decoding = 'async';
        image.onload = function () {
          resolve(image);
        };
        image.onerror = function () {
          reject(new Error('ไม่สามารถโหลดรูปอ้างอิงจาก ' + url));
        };
        image.src = url;
      });
    }

    function readFileAsDataUrl(file) {
      return new Promise(function (resolve, reject) {
        var reader = new FileReader();
        reader.onload = function () {
          resolve(String(reader.result || ''));
        };
        reader.onerror = function () {
          reject(new Error(messages.registerFaceFailed));
        };
        reader.readAsDataURL(file);
      });
    }

    function getKnownFaceRegistrationSource() {
      if (snapshotField.value) {
        return {
          kind: 'snapshot',
          value: snapshotField.value
        };
      }

      if (scanUploadInput.files && scanUploadInput.files.length > 0) {
        return {
          file: scanUploadInput.files[0],
          kind: 'scan-upload'
        };
      }

      if (uploadInput.files && uploadInput.files.length > 0) {
        return {
          file: uploadInput.files[0],
          kind: 'upload'
        };
      }

      var imageUrl = imageUrlInput.value ? String(imageUrlInput.value).trim() : '';

      if (imageUrl) {
        return {
          kind: 'url',
          value: imageUrl
        };
      }

      return null;
    }

    async function loadRegistrationImageElement() {
      var source = getKnownFaceRegistrationSource();

      if (!source) {
        throw new Error(messages.registerFaceMissing);
      }

      if (source.kind === 'snapshot' || (source.value && String(source.value).indexOf('data:image/') === 0)) {
        return loadImageElement(source.value);
      }

      if (source.file) {
        var fileDataUrl = await readFileAsDataUrl(source.file);
        return loadImageElement(fileDataUrl);
      }

      return loadImageElement(source.value);
    }

    async function buildRegistrationDescriptorValues() {
      await loadModels();
      var image = await loadRegistrationImageElement();
      var detection = await window.faceapi
        .detectSingleFace(image, detectorOptions)
        .withFaceLandmarks(true)
        .withFaceDescriptor();

      if (!detection || !detection.descriptor) {
        throw new Error(messages.registerFaceNotFound);
      }

      return Array.from(detection.descriptor);
    }

    async function requestFaceMatches(results, sessionId) {
      if (!Array.isArray(results) || !results.length || !hasKnownFaceDatabase()) {
        return;
      }

      if (sessionId !== state.scanSessionId) {
        return;
      }

      if (!config.faceMatchApiUrl || !window.fetch) {
        throw new Error(messages.matchApiFailed);
      }

      if (state.matchApiInFlightToken) {
        return;
      }

      var now = Date.now();

      if (now - state.lastMatchRequestAt < matchRequestIntervalMs) {
        return;
      }

      var descriptorPayloads = [];

      for (var index = 0; index < results.length; index += 1) {
        var descriptorValues = descriptorToArray(results[index] && results[index].descriptor);

        if (descriptorValues.length !== 128) {
          return;
        }

        descriptorPayloads.push(descriptorValues);
      }

      var requestToken = state.lastMatchRequestSequence + 1;
      state.lastMatchRequestSequence = requestToken;
      state.matchApiInFlightToken = requestToken;
      state.lastMatchRequestAt = now;

      try {
        var response = await window.fetch(config.faceMatchApiUrl, {
          body: JSON.stringify({
            descriptors: descriptorPayloads,
            threshold: recognitionThreshold
          }),
          credentials: 'same-origin',
          headers: {
            'Content-Type': 'application/json'
          },
          method: 'POST'
        });

        if (!response.ok) {
          var errorMessage = messages.matchApiFailed;

          try {
            var errorPayload = await response.json();

            if (errorPayload && errorPayload.message) {
              errorMessage = String(errorPayload.message);
            }
          } catch (parseError) {
            // Ignore JSON parse errors from the fallback path.
          }

          throw new Error(errorMessage);
        }

        var payload = await response.json();

        if (state.matchApiInFlightToken !== requestToken || sessionId !== state.scanSessionId) {
          return;
        }

        state.loadedKnownFaceCount = Math.max(0, Number(payload.personCount || payload.PersonCount || 0));
        state.loadedKnownFaceSampleCount = Math.max(0, Number(payload.sampleCount || payload.SampleCount || 0));
        updateKnownFaceSummary();

        var rawMatches = Array.isArray(payload.matches || payload.Matches) ? payload.matches || payload.Matches : [];
        var normalizedMatches = [];

        for (var matchIndex = 0; matchIndex < rawMatches.length; matchIndex += 1) {
          normalizedMatches.push(normalizeServerMatch(rawMatches[matchIndex]));
        }

        while (normalizedMatches.length < descriptorPayloads.length) {
          normalizedMatches.push({
            distance: 1,
            label: 'unknown',
            pending: false
          });
        }

        state.lastMatches = normalizedMatches.slice(0, descriptorPayloads.length);
      } finally {
        if (state.matchApiInFlightToken === requestToken) {
          state.matchApiInFlightToken = 0;
        }
      }
    }

    async function loadModels() {
      if (state.modelsLoaded) {
        return;
      }

      if (!window.faceapi) {
        throw new Error(messages.faceApiMissing);
      }

      setStatus(messages.loadingModels, 'info');

      await Promise.all([
        window.faceapi.nets.tinyFaceDetector.loadFromUri(config.modelPath),
        window.faceapi.nets.faceLandmark68TinyNet.loadFromUri(config.modelPath),
        window.faceapi.nets.faceRecognitionNet.loadFromUri(config.modelPath)
      ]);

      detectorOptions = new window.faceapi.TinyFaceDetectorOptions({
        inputSize: 320,
        scoreThreshold: 0.5
      });

      state.modelsLoaded = true;
      setStatus(messages.modelsReady, 'success');
    }

    function isSecureCameraContext() {
      return (
        window.isSecureContext ||
        window.location.hostname === 'localhost' ||
        window.location.hostname === '127.0.0.1'
      );
    }

    async function startScanner() {
      if (state.running) {
        setStatus(messages.scannerAlreadyRunning, 'info');
        return;
      }

      if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
        setStatus(messages.browserNotSupported, 'error');
        return;
      }

      if (!isSecureCameraContext()) {
        setStatus(messages.secureContextRequired, 'error');
        return;
      }

      try {
        state.autoSaveInProgress = false;
        state.lastMatchRequestAt = 0;
        state.lastMatches = [];
        state.matchApiInFlightToken = 0;
        state.scanSessionId += 1;
        resetPendingRecognizedMatch();
        recognizedSnapshotField.value = '';
        recognizedNameField.value = '';
        await loadModels();
        refreshKnownFaceCatalogSummary();

        state.stream = await navigator.mediaDevices.getUserMedia({
          audio: false,
          video: {
            facingMode: 'user',
            height: { ideal: 720 },
            width: { ideal: 1280 }
          }
        });

        video.srcObject = state.stream;
        await video.play();

        state.running = true;
        emptyState.style.display = 'none';

        if (hasKnownFaceDatabase()) {
          setStatus(messages.cameraAndMatchRunning, 'success');
        } else {
          setStatus(messages.cameraRunning, 'success');
        }

        updateStats([], messages.scannerRunning);
        runDetectionLoop();
      } catch (error) {
        var message = getFriendlyErrorMessage(error, messages.openCameraFailed);
        setStatus(message, 'error');
      }
    }

    function stopStreamTracks() {
      if (!state.stream) {
        return;
      }

      state.stream.getTracks().forEach(function (track) {
        track.stop();
      });

      state.stream = null;
    }

    function stopScanner() {
      state.autoSaveInProgress = false;
      state.running = false;
      state.lastMatches = [];
      state.lastMatchRequestAt = 0;
      state.lastResults = [];
      state.matchApiInFlightToken = 0;
      state.scanSessionId += 1;
      resetPendingRecognizedMatch();

      if (state.detectionTimer) {
        window.clearTimeout(state.detectionTimer);
        state.detectionTimer = 0;
      }

      stopStreamTracks();
      video.pause();
      video.srcObject = null;
      clearOverlay();
      emptyState.style.display = 'flex';
      updateStats([], messages.scannerStoppedMode);
      updateRecognitionSummary([]);
      setStatus(messages.scannerStopped, 'warning');
    }

    function findBestRecognizedMatch(matches) {
      if (!Array.isArray(matches) || !matches.length) {
        return null;
      }

      var bestRecognizedMatch = null;

      matches.forEach(function (match, index) {
        if (!match || !match.label || match.label === 'unknown') {
          return;
        }

        if (!bestRecognizedMatch || match.distance < bestRecognizedMatch.match.distance) {
          bestRecognizedMatch = {
            index: index,
            match: match,
            result: state.lastResults[index] || null
          };
        }
      });

      return bestRecognizedMatch;
    }

    async function runDetectionLoop() {
      if (!state.running) {
        return;
      }

      var sessionId = state.scanSessionId;
      var displaySize = ensureOverlaySize();

      if (!displaySize) {
        state.detectionTimer = window.setTimeout(runDetectionLoop, 120);
        return;
      }

      try {
        var results = await window.faceapi
          .detectAllFaces(video, detectorOptions)
          .withFaceLandmarks(true)
          .withFaceDescriptors();

        if (sessionId !== state.scanSessionId || !state.running) {
          return;
        }

        state.lastResults = results || [];

        if (!state.lastResults.length) {
          state.lastMatches = [];
        } else {
          if (!Array.isArray(state.lastMatches) || state.lastMatches.length !== state.lastResults.length) {
            state.lastMatches = buildPendingMatches(state.lastResults.length);
          }

          try {
            await requestFaceMatches(state.lastResults, sessionId);
          } catch (error) {
            state.lastMatches = buildUnknownMatches(state.lastResults.length);
            setStatus(getFriendlyErrorMessage(error, messages.matchApiFailed), 'error');
          }
        }

        if (sessionId !== state.scanSessionId || !state.running) {
          return;
        }

        drawResults(state.lastResults, displaySize, state.lastMatches);
        updateRecognitionSummary(state.lastMatches);

        if (!state.lastResults.length) {
          resetPendingRecognizedMatch();
          updateStats([], messages.noFaceInFrame);
          setStatus(messages.noFaceInCurrentFrame, 'warning');
        } else {
          var bestRecognizedMatch = findBestRecognizedMatch(state.lastMatches);

          if (bestRecognizedMatch) {
            updateStats(state.lastResults, 'พบ ' + bestRecognizedMatch.match.label);
            setStatus('ตรวจพบ ' + bestRecognizedMatch.match.label, 'success');
            handleRecognizedAutoSave(bestRecognizedMatch);
          } else if (hasKnownFaceDatabase()) {
            resetPendingRecognizedMatch();
            updateStats(
              state.lastResults,
              state.lastResults.length === 1 ? messages.detectedOneFace : messages.detectedManyFaces
            );
            setStatus(
              state.lastMatches.some(function (match) {
                return match && match.pending;
              })
                ? messages.matchingOnServer
                : messages.faceDetectedButUnknown,
              state.lastMatches.some(function (match) {
                return match && match.pending;
              })
                ? 'info'
                : 'warning'
            );
          } else if (state.lastResults.length === 1) {
            resetPendingRecognizedMatch();
            updateStats(state.lastResults, messages.detectedOneFace);
            setStatus(messages.oneFaceDetected, 'success');
          } else {
            resetPendingRecognizedMatch();
            updateStats(state.lastResults, messages.detectedManyFaces);
            setStatus(messages.manyFacesDetected, 'warning');
          }
        }
      } catch (error) {
        resetPendingRecognizedMatch();
        var message = getFriendlyErrorMessage(error, messages.scanFailed);
        setStatus(message, 'error');
      }

      if (state.running && sessionId === state.scanSessionId) {
        state.detectionTimer = window.setTimeout(runDetectionLoop, 120);
      }
    }

    function captureSnapshot() {
      if (!state.running || !video.videoWidth || !video.videoHeight) {
        setStatus(messages.captureOpenCameraFirst, 'error');
        return;
      }

      var dataUrl = buildSnapshotDataUrl(null);
      snapshotField.value = dataUrl;
      snapshotPreview.src = dataUrl;
      snapshotPreviewWrap.style.display = 'block';
      setSaveButtonEnabled(true);

      if (state.lastResults.length) {
        setStatus(messages.captureReady, 'success');
      } else {
        setStatus(messages.captureNoFace, 'warning');
      }
    }

    function handleSaveButtonClick(event) {
      if (snapshotField.value) {
        return;
      }

      event.preventDefault();
      setStatus(messages.saveSnapshotFirst, 'error');
    }

    async function handleRegisterButtonClick(event) {
      if (state.registerPostbackReady) {
        state.registerPostbackReady = false;
        return;
      }

      event.preventDefault();

      if (state.registerDescriptorBusy) {
        return;
      }

      state.registerDescriptorBusy = true;
      knownFaceDescriptorField.value = '';
      setStatus(messages.registerFaceLoading, 'info');

      try {
        var descriptorValues = await buildRegistrationDescriptorValues();
        knownFaceDescriptorField.value = JSON.stringify(descriptorValues);
        setStatus(messages.registerFaceReady, 'success');
        state.registerPostbackReady = true;
        registerButton.click();
      } catch (error) {
        var message = getFriendlyErrorMessage(error, messages.registerFaceFailed);
        setStatus(message, 'error');
      } finally {
        state.registerDescriptorBusy = false;
      }
    }

    function cleanup() {
      stopScanner();
    }

    state.knownFaceSummary = parseKnownFaceCatalogSummary();
    knownFaceDescriptorField.value = '';
    recognizedSnapshotField.value = '';
    recognizedNameField.value = '';
    refreshKnownFaceCatalogSummary();
    updateKnownFaceSummary();
    setSaveButtonEnabled(false);
    updateStats([], messages.ready);
    updateRecognitionSummary([]);
    setStatus(messages.notStarted, 'info');

    startButton.addEventListener('click', function () {
      startScanner();
    });

    stopButton.addEventListener('click', function () {
      stopScanner();
    });

    captureButton.addEventListener('click', function () {
      captureSnapshot();
    });

    saveButton.addEventListener('click', handleSaveButtonClick);
    registerButton.addEventListener('click', function (event) {
      handleRegisterButtonClick(event);
    });
    window.addEventListener('beforeunload', cleanup);

    if (resumeField.value === '1') {
      resumeField.value = '';
      window.setTimeout(function () {
        startScanner();
      }, 300);
    }
  });
})();
