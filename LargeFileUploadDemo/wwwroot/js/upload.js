var UploadPath = "";
var splitSize = 2 * 1024;    // 切片大小

function startUpload() {
    var file = $("#path")[0].files[0];
    ajaxUploadFile(file, 0);
}

function ajaxUploadFile(file, i) {
    var name = file.name;
    size = file.size;
    splitCount = Math.ceil(size / splitSize);
    if (i > splitCount) {
        return;
    }
    // 计算分片的起始位置和结束位置
    start = i * splitSize;
    end = Math.min(size, start + splitSize);
    // 构造表单
    var form = new FormData();
    form.append("data", file.slice(start, end));
    form.append("lastModified", file.lastModified);
    form.append("filename", file.name);
    form.append("total", splitCount);
    form.append("index", i);
    UploadPath = file.lastModified;
    $.ajax({
        url: "/Home/Upload",
        type: "POST",
        data: form,
        processData: false, // 告诉jquery不要对form进行处理
        contentType: false,
        success: function (result) {
            if (result != null) {
                i = result.number++;
                var num = Math.ceil(i * 100 / splitCount);
                $("#output").text(num + '%');
                ajaxUploadFile(file, i);
                if (result.mergeOk) {
                    var filepath = $("#path");
                    filepath.after(filepath.clone().val(""));
                    filepath.remove();
                    $("#uploadFile").val("请选择文件");
                    alert("success");
                }
            }
        }
    })
}

