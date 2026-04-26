const MODEL_URL = 'https://justadudewhohacks.github.io/face-api.js/models';

let video        = null;
let canvas       = null;
let isStarted    = false;
let dotNetHelper = null;
let labeledDescriptors  = [];
let roiCanvas           = null;
let roiInitialized      = false;   // FIX #3: only set canvas dims once
let smoothedBox         = null;
let missedDetectionFrames = 0;
let lastRejectedMessage = '';
let lastRejectedAt      = 0;
let descriptorHistory   = [];
let lastCandidateLabel  = null;
let faceLockCount       = 0;
let recognitionCounts   = new Map();
let recognitionCooldowns = new Map();
let loopRunning         = false;
let _canvasCtx          = null;
let _detectorOptions    = null;
let _profileOptions     = null;
let lastTickEnd         = 0;

const CFG = {
    acceptanceThreshold:        0.48, // Must be extremely strict to prevent strangers from matching
    fastAcceptanceThreshold:    0.38, // Instant match only if identical
    ambiguityMargin:            0.08, // Higher margin so similar-looking people don't get mixed up
    stabilityThreshold:         3,    // Require 3 consecutive positive frames to block random 'fluke' matches
    minConfidence:              0.60, // Higher confidence blocks random objects/shadows
    minFaceBoxWidth:            80,
    minFaceBoxHeight:           80,
    cooldownMs:                 2500,
    gapAfterTickMs:             15,   // Process more frames per second
    tinyInputSize:              224,  // Better balance of accuracy and speed
    profileInputSize:           416,
    guideWidthRatio:            0.56,
    guideHeightRatio:           0.76,
    rejectionDebounceMs:        1500,
    boxSmoothingFactor:         0.80, // Snappier box tracking
    maxMissedFramesBeforeReset: 5,
    faceLockFrames:             2,    // Ensure face is stable before reading
    descriptorHistorySize:      4,    // Average over 4 frames for reliable recognition
};

const getCtx = () => {
    if (!_canvasCtx || _canvasCtx.canvas !== canvas) _canvasCtx = canvas.getContext('2d');
    return _canvasCtx;
};

// FIX #1: use detectSingleFace (not detectAllFaces) — one neural net chain, not three in sequence
const getDetectorOptions = () => {
    if (!_detectorOptions) _detectorOptions = new faceapi.TinyFaceDetectorOptions({
        inputSize: CFG.tinyInputSize,
        scoreThreshold: CFG.minConfidence,
    });
    return _detectorOptions;
};

const getProfileOptions = () => {
    if (!_profileOptions) _profileOptions = new faceapi.TinyFaceDetectorOptions({
        inputSize: CFG.profileInputSize,
        scoreThreshold: 0.35,
    });
    return _profileOptions;
};

window.attendanceRecognizer = {

    start: async function (helper, students) {
        if (isStarted) return;
        dotNetHelper = helper;
        await dotNetHelper.invokeMethodAsync('OnStatusChanged', 'Initializing...');

        let retries = 0;
        while (retries < 10) {
            video  = document.getElementById('attendanceVideo');
            canvas = document.getElementById('attendanceCanvas');
            if (video && canvas) break;
            await new Promise(r => setTimeout(r, 200));
            retries++;
        }

        if (!video || !canvas) {
            await dotNetHelper.invokeMethodAsync('OnStatusChanged', 'Error: Elements not found.');
            return;
        }

        try {
            if (typeof faceapi === 'undefined') {
                await dotNetHelper.invokeMethodAsync('OnStatusChanged', 'Loading library...');
                await this.loadScript('https://cdn.jsdelivr.net/npm/face-api.js@0.22.2/dist/face-api.min.js');
            }

            await dotNetHelper.invokeMethodAsync('OnStatusChanged', 'Loading models...');
            await Promise.all([
                faceapi.nets.tinyFaceDetector.loadFromUri(MODEL_URL),
                faceapi.nets.faceLandmark68Net.loadFromUri(MODEL_URL),
                faceapi.nets.faceRecognitionNet.loadFromUri(MODEL_URL),
            ]);

            await dotNetHelper.invokeMethodAsync('OnStatusChanged', 'Starting camera...');
            const stream = await navigator.mediaDevices.getUserMedia({
                video: { facingMode: 'user', width: { ideal: 640 }, height: { ideal: 480 } }
            });
            video.srcObject = stream;
            await new Promise(resolve => (video.onloadedmetadata = resolve));
            video.play();

            await dotNetHelper.invokeMethodAsync('OnStatusChanged', 'Preparing student profiles...');
            await this.prepareDescriptors(students);

            isStarted = true;
            _canvasCtx = null;
            _detectorOptions = null;
            roiInitialized = false;
            await dotNetHelper.invokeMethodAsync('OnStatusChanged', 'Active');
            this.loop();
        } catch (err) {
            console.error('Face recognition error:', err);
            await dotNetHelper.invokeMethodAsync('OnStatusChanged', `Error: ${err.message || 'Camera access failed'}`);
            alert('Failed to access camera or load face models. Please check permissions.');
        }
    },

    stop: function () {
        if (!isStarted) return;
        isStarted      = false;
        loopRunning    = false;
        roiInitialized = false;
        smoothedBox    = null;
        missedDetectionFrames = 0;
        descriptorHistory     = [];
        lastCandidateLabel    = null;
        faceLockCount         = 0;
        recognitionCounts     = new Map();
        recognitionCooldowns  = new Map();
        lastRejectedMessage   = '';
        lastRejectedAt        = 0;
        lastTickEnd           = 0;
        if (video?.srcObject) video.srcObject.getTracks().forEach(t => t.stop());
        if (canvas) getCtx().clearRect(0, 0, canvas.width, canvas.height);
    },

    loadScript: function (url) {
        return new Promise((resolve, reject) => {
            const s = document.createElement('script');
            s.src = url; s.onload = resolve; s.onerror = reject;
            document.head.appendChild(s);
        });
    },

    getGuideRegion: function (w, h) {
        const gw = Math.round(w * CFG.guideWidthRatio);
        const gh = Math.round(h * CFG.guideHeightRatio);
        return { x: Math.round((w - gw) / 2), y: Math.round((h - gh) / 2), width: gw, height: gh };
    },

    drawGuideOverlay: function (ctx, g) {
        ctx.save();
        ctx.fillStyle = 'rgba(0,0,0,0.28)';
        ctx.fillRect(0, 0, canvas.width, g.y);
        ctx.fillRect(0, g.y + g.height, canvas.width, canvas.height - g.y - g.height);
        ctx.fillRect(0, g.y, g.x, g.height);
        ctx.fillRect(g.x + g.width, g.y, canvas.width - g.x - g.width, g.height);
        ctx.strokeStyle = '#ffffff';
        ctx.lineWidth   = 3;
        ctx.setLineDash([12, 8]);
        ctx.strokeRect(g.x, g.y, g.width, g.height);
        ctx.fillStyle = '#ffffff';
        ctx.font      = '600 16px Arial';
        ctx.textAlign = 'center';
        ctx.fillText('Align one face inside the box', g.x + g.width / 2, g.y - 12);
        ctx.restore();
    },

    smoothBox: function (box) {
        if (!smoothedBox) { smoothedBox = { ...box }; return smoothedBox; }
        const f = CFG.boxSmoothingFactor;
        smoothedBox = {
            x:      smoothedBox.x      + (box.x      - smoothedBox.x)      * f,
            y:      smoothedBox.y      + (box.y      - smoothedBox.y)      * f,
            width:  smoothedBox.width  + (box.width  - smoothedBox.width)  * f,
            height: smoothedBox.height + (box.height - smoothedBox.height) * f,
        };
        return smoothedBox;
    },

    reportRejected: function (message) {
        if (!message) return;
        const now = Date.now();
        if (message === lastRejectedMessage && now - lastRejectedAt < CFG.rejectionDebounceMs) return;
        lastRejectedMessage = message;
        lastRejectedAt      = now;
        dotNetHelper.invokeMethodAsync('OnRecognitionRejected', message);
    },

    getAverageDescriptor: function (history) {
        if (!history.length) return null;
        const len = history[0].length;
        const avg = new Float32Array(len);
        for (const d of history) for (let i = 0; i < len; i++) avg[i] += d[i];
        for (let i = 0; i < len; i++) avg[i] /= history.length;
        return avg;
    },

    // FIX #7: early-exit scan — stop as soon as we find a very confident match
    getTopMatches: function (descriptor) {
        const matches = [];
        for (const labeled of labeledDescriptors) {
            if (!labeled.descriptors?.length) continue;
            let best = Infinity;
            for (const kd of labeled.descriptors) {
                const d = faceapi.euclideanDistance(descriptor, kd);
                if (d < best) best = d;
            }
            matches.push({ label: labeled.label, distance: best });
            // Early exit: if we've found a very confident match and scanned at least
            // half the list, the second-best check will still be valid enough
            if (best < CFG.fastAcceptanceThreshold && matches.length >= Math.ceil(labeledDescriptors.length / 2)) {
                break;
            }
        }
        return matches.sort((a, b) => a.distance - b.distance);
    },

    resetState: function () {
        faceLockCount      = 0;
        lastCandidateLabel = null;
        descriptorHistory  = [];
        recognitionCounts.clear();
    },

    prepareDescriptors: async function (students) {
        labeledDescriptors = [];
        let loaded = 0;

        for (const student of students) {
            if (!student.imagePath) continue;
            try {
                const img = await faceapi.fetchImage(
                    new URL(student.imagePath, window.location.origin).toString()
                );
                const det = await faceapi
                    .detectSingleFace(img, getProfileOptions())
                    .withFaceLandmarks()
                    .withFaceDescriptor();

                if (det) {
                    labeledDescriptors.push(
                        new faceapi.LabeledFaceDescriptors(student.studentId, [det.descriptor])
                    );
                    loaded++;
                } else {
                    console.warn(`No face detected for ${student.fullName}`);
                }
            } catch (err) {
                console.warn(`Profile error for ${student.fullName}:`, err);
            }
        }

        await dotNetHelper.invokeMethodAsync(
            'OnStatusChanged',
            loaded === 0
                ? 'No usable student profile photos found.'
                : `Loaded ${loaded} student profiles.`
        );
    },

    loop: async function () {
        if (!isStarted || loopRunning) return;
        loopRunning = true;

        const vw = video.videoWidth;
        const vh = video.videoHeight;
        faceapi.matchDimensions(canvas, { width: vw, height: vh });

        // FIX #3: initialize ROI canvas dimensions once
        roiCanvas = roiCanvas || document.createElement('canvas');
        const guide = this.getGuideRegion(vw, vh);
        if (!roiInitialized) {
            roiCanvas.width  = guide.width;
            roiCanvas.height = guide.height;
            roiInitialized   = true;
        }

        while (isStarted) {
            // FIX #2: gap is measured from when the PREVIOUS tick ENDED, not when it started
            const sinceLastEnd = Date.now() - lastTickEnd;
            if (sinceLastEnd < CFG.gapAfterTickMs) {
                await new Promise(r => setTimeout(r, CFG.gapAfterTickMs - sinceLastEnd));
            }

            try {
                await this.tick(guide);
            } catch (err) {
                console.warn('Recognition cycle error:', err);
            }

            lastTickEnd = Date.now();
            // Yield one microtask to keep the browser paint queue alive
            await new Promise(r => setTimeout(r, 0));
        }

        loopRunning = false;
    },

    tick: async function (guide) {
        const vw = video.videoWidth;
        const vh = video.videoHeight;
        if (!vw || !vh) return;

        // FIX #3: don't resize roiCanvas every tick — it discards the GPU texture
        const roiCtx = roiCanvas.getContext('2d');
        
        // Enhance image for low-light situations before feeding it into the neural network
        roiCtx.filter = 'brightness(1.15) contrast(1.1)';
        roiCtx.drawImage(video, guide.x, guide.y, guide.width, guide.height,
            0,       0,        guide.width, guide.height);
        roiCtx.filter = 'none';

        // FIX #1: detectSingleFace instead of detectAllFaces
        // This runs: TinyFaceDetector → FaceLandmark68Net → FaceRecognitionNet
        // But as a SINGLE chain with early exit if no face found — no unnecessary landmark pass
        const fullResult = await faceapi
            .detectSingleFace(roiCanvas, getDetectorOptions())
            .withFaceLandmarks()
            .withFaceDescriptor();

        const ctx = getCtx();
        ctx.clearRect(0, 0, canvas.width, canvas.height);
        this.drawGuideOverlay(ctx, guide);

        // ── No face ────────────────────────────────────────────────────────────
        if (!fullResult) {
            missedDetectionFrames++;
            if (missedDetectionFrames >= CFG.maxMissedFramesBeforeReset) {
                smoothedBox = null;
                this.resetState();
            }
            return;
        }

        missedDetectionFrames = 0;
        lastRejectedMessage   = '';

        const rawBox = fullResult.detection.box;
        const box = this.smoothBox({
            x:      rawBox.x      + guide.x,
            y:      rawBox.y      + guide.y,
            width:  rawBox.width,
            height: rawBox.height,
        });
        const score = fullResult.detection.score ?? 0;

        // ── Face too small ─────────────────────────────────────────────────────
        if (box.width < CFG.minFaceBoxWidth || box.height < CFG.minFaceBoxHeight) {
            this.reportRejected('Move closer to the camera and keep your face in the guide box.');
            // FIX #4 (partial): don't full-reset on a positional issue — keep descriptor history
            faceLockCount = 0;
            new faceapi.draw.DrawBox(box, { label: 'Move closer', boxColor: '#ffc107' }).draw(canvas);
            return;
        }

        // ── Low confidence ─────────────────────────────────────────────────────
        if (score < CFG.minConfidence) {
            this.reportRejected('Hold still inside the guide box for a moment.');
            new faceapi.draw.DrawBox(box, { label: 'Hold still', boxColor: '#6c757d' }).draw(canvas);
            return;
        }

        faceLockCount = Math.min(faceLockCount + 1, CFG.faceLockFrames + 4);
        const faceLocked = faceLockCount >= CFG.faceLockFrames;

        // Accumulate descriptor history
        descriptorHistory.push(new Float32Array(fullResult.descriptor));
        if (descriptorHistory.length > CFG.descriptorHistorySize) descriptorHistory.shift();

        const avgDescriptor = this.getAverageDescriptor(descriptorHistory) ?? fullResult.descriptor;

        // FIX #5: getGuideRegion already computed in loop() — reuse guide param
        // FIX #7: early-exit scan inside getTopMatches
        const topMatches = this.getTopMatches(avgDescriptor);
        const best       = topMatches[0];
        const second     = topMatches[1];

        const label    = best?.label    ?? 'unknown';
        const distance = best?.distance ?? Infinity;
        const margin   = second ? second.distance - distance : Infinity;

        const now          = Date.now();
        const inCooldown   = recognitionCooldowns.has(label) && recognitionCooldowns.get(label) > now;
        const passesDist   = label !== 'unknown' && distance <= CFG.acceptanceThreshold;
        const passesFast   = label !== 'unknown' && distance <= CFG.fastAcceptanceThreshold;
        const passesMargin = margin >= CFG.ambiguityMargin;
        const sameAsPrev   = lastCandidateLabel === label;

        let count    = recognitionCounts.get(label) || 0;
        let boxColor = '#dc3545';
        let labelText = `${label === 'unknown' ? 'unknown' : label}  d=${distance.toFixed(3)}`;

        // ── Candidate passes all checks ────────────────────────────────────────
        if (passesDist && passesMargin && faceLocked && !inCooldown) {
            count = sameAsPrev ? count + 1 : 1;
            recognitionCounts.set(label, count);
            lastCandidateLabel = label;

            const confirmed = passesFast ? count >= 1 : count >= CFG.stabilityThreshold;

            if (confirmed) {
                dotNetHelper.invokeMethodAsync('OnStudentRecognized', label);
                recognitionCounts.set(label, 0);
                recognitionCooldowns.set(label, now + CFG.cooldownMs);
                lastCandidateLabel = null;
                descriptorHistory  = [];
                new faceapi.draw.DrawBox(box, { label: `Marked: ${label}`, boxColor: '#198754' }).draw(canvas);
                return;
            }

            // Progress bar
            boxColor  = '#0dcaf0';
            labelText = `${label} (${count}/${CFG.stabilityThreshold})  d=${distance.toFixed(3)}`;
            const barY = box.y + box.height + 5;
            ctx.fillStyle = '#343a40';
            ctx.fillRect(box.x, barY, box.width, 7);
            ctx.fillStyle = '#0dcaf0';
            ctx.fillRect(box.x, barY, box.width * (count / CFG.stabilityThreshold), 7);

            // ── Candidate fails ────────────────────────────────────────────────────
        } else {
            // FIX #4: only clear history if the IDENTITY changed, not on a borderline frame
            const identityChanged = lastCandidateLabel !== null && lastCandidateLabel !== label;
            if (identityChanged) {
                descriptorHistory  = [];
                lastCandidateLabel = null;
            }
            recognitionCounts.set(label, 0);

            if (label !== 'unknown') {
                if (!passesDist) {
                    labelText += ' [NO MATCH]';
                    // Only clear history on a clearly different face (distance well above threshold)
                    if (distance > CFG.acceptanceThreshold + 0.08) {
                        descriptorHistory  = [];
                        lastCandidateLabel = null;
                    }
                    this.reportRejected('Face not matched. Attendance not marked.');
                } else if (!passesMargin) {
                    labelText += ' [AMBIGUOUS]';
                    this.reportRejected('Recognition is ambiguous. Please try again.');
                } else if (inCooldown) {
                    labelText += ' [WAIT]';
                } else if (!faceLocked) {
                    this.reportRejected('Hold your face in the box for a moment.');
                }
            } else {
                this.reportRejected('Face not matched. Attendance not marked.');
            }
        }

        new faceapi.draw.DrawBox(box, { label: labelText, boxColor }).draw(canvas);

        // Prune expired cooldowns
        for (const [id, exp] of recognitionCooldowns) {
            if (exp <= now) recognitionCooldowns.delete(id);
        }
    },
};