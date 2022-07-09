function uploadClicked() {
    if ($("a:hover").length == 0) {
        $('#inputUpload').click();
    }
}
function uploadFiles(fileList) {
    if ((fileList || []).length == 0) {
        return;
    }

    showLoading();
    var file = fileList[0];
    var apiPath = "/api/file/";
    var fd = new FormData();
    fd.append('file', file);
    var xhr = new XMLHttpRequest();
    xhr.open('POST', apiPath, true);
    xhr.addEventListener("load", function () {
        if (xhr.status === 200) {
            var downloadURL = xhr.responseText;
            $("#divDropMessage").html('Download Link: <a href="' + downloadURL + '">' + downloadURL + '</a><br/><br/><button class="btn btn-secondary">New File</button>');
        }
        else {
            $("#divDropMessage").html("File upload failed.");
        }
        removeLoading();
    });
    
    xhr.upload.addEventListener("progress", function (e) {
        var percentLoaded = Math.round(e.loaded / file.size * 100).toString();
        document.getElementById("loadingProgressLabel").innerHTML = percentLoaded + "%";
    });
    xhr.send(fd);
}

function showLoading() {
    var frame = document.createElement("div");
    frame.classList.add("loading-frame");
    frame.id = "divLoadingFrame";
    var progressLabel = document.createElement("div");
    progressLabel.id = "loadingProgressLabel";
    frame.appendChild(progressLabel);
    for (var i = 0; i < 10; i++) {
        var track = document.createElement("div");
        track.classList.add("loading-track");
        var dot = document.createElement("div");
        dot.classList.add("loading-dot");
        track.style.transform = "rotate(" + String(i * 36) + "deg)";
        track.appendChild(dot);
        frame.appendChild(track);
    }
    document.body.appendChild(frame);
    var wait = 0;
    $(frame).find(".loading-dot").each(function (index, elem) {
        elem.style.animationDelay = String(wait) + "ms";
        elem.classList.add("loading-dot-animated");
        wait += 150;
    });
}

function removeLoading() {
    $("#divLoadingFrame").remove();
}

window.ondragover = function (e) {
    e.preventDefault();
    e.dataTransfer.dropEffect = "copy";
};
window.ondrop = function (e) {
    e.preventDefault();
    if (e.dataTransfer.files.length < 1) {
        return;
    }
    uploadFiles(e.dataTransfer.files);
};
