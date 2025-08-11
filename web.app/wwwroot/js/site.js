
async function onCommandClick(_this, _id, cmd) {
	_this.disabled = true;
	_old = _this.innerHTML;
	_this.innerHTML = '<span class="spinner-border spinner-border-sm" aria-hidden="true"></span>';
	try {
		msgbox(`${_id}<hr />任务已经下发`)
		const response = await fetch(`/worker/control?id=${_id}&cmd=${cmd}`);
		if (!response.ok) {
			throw new Error(`HTTP error! status: ${response.status}`);
		}
		var obj = await response.json();
		if (obj.code != 200) {
			throw new Error(`error! status: ${obj.code}`);
		}
		if (obj.url != null) {
			msgbox(obj.url);
			window.location = obj.url;
		}
	} catch (e) {
		_this.innerHTML = _old;
		_this.disabled = false;
		console.log(e);
		msgbox(error.message)
	}
}

$(function () {
	$("body").on('click', '*[data-toggle="true"]', function (e) {
		$('*[data-toggle="true"]').toggle()
	})
	$("body").on('submit', 'form[method="post"][data-bs-modal]', function (e) {
		e.preventDefault()
		let btn = $(this).find('button[type="submit"]').attr('disabled', true)
		let old = btn.html()
		btn.html('<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span>&nbspLoading...')
		$.post($(this).attr('action'), $(this).serialize(), function (data) {
			btn.html(old).attr('disabled', false)
			if ($.isPlainObject(data)) {
				if ($.isEmptyObject(data.url)) {
					msgbox(data.message)
				} else {
					if (data.url == '#') {
						location.reload()
					} else {
						location = data.url
					}
				}
			} else {
				$('.modal.show .modal-content').html(data)
			}
		})
	})
	document.querySelectorAll('[id^="remote-modal"]').forEach(item => {
		item.addEventListener('show.bs.modal', event => {
			let content = event.target.querySelector('.modal-content')
			_html = content.outerHTML
			try {
				$(content).load(event.relatedTarget.href)
			} catch (error) {
				msgbox(error.message)
			}
		})
		item.addEventListener('hidden.bs.modal', event => {
			if (document.activeElement.classList.contains('btn-close-refresh')) {
				location.reload()
			}
			event.target.querySelector('.modal-content').outerHTML = _html
		})
	})
})