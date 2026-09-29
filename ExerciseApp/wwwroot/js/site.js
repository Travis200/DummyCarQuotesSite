const quoteForm = document.querySelector('#quote-form');
const insuranceTypeSelect = document.querySelector('#insurance-type');
const dateOfBirthInput = document.querySelector('#date-of-birth');
const makeSelect = document.querySelector('#make');
const modelSelect = document.querySelector('#model');
const submitButton = document.querySelector('#submit-button');
const quoteResult = document.querySelector('#quote-result');
const historyCountSelect = document.querySelector('#history-count');
const historyList = document.querySelector('#history-list');
const currentYear = document.querySelector('#year');

let modelsByMake = [];

currentYear.textContent = new Date().getFullYear();
dateOfBirthInput.max = new Date().toISOString().slice(0, 10);

function escapeHtml(value) {
  return String(value ?? '').replace(/[&<>"']/g, character => ({
    '&': '&amp;',
    '<': '&lt;',
    '>': '&gt;',
    '"': '&quot;',
    "'": '&#39;'
  })[character]);
}

async function readJson(response) {
  const body = await response.json().catch(() => null);
  if (!response.ok) {
    throw new Error(body?.errorMessage || body?.title || 'The request could not be completed. Please try again.');
  }
  return body;
}

function formatInsuranceType(value) {
  return String(value ?? '').replace(/([a-z])([A-Z])/g, '$1 $2');
}

async function loadQuoteOptions() {
  try {
    const detail = await readJson(await fetch('/Quote'));
    modelsByMake = detail.models ?? [];

    for (const insurance of detail.insuranceTypes ?? []) {
      const option = document.createElement('option');
      option.value = insurance.type;
      option.textContent = insurance.description || formatInsuranceType(insurance.type);
      insuranceTypeSelect.append(option);
    }

    for (const make of detail.makes ?? []) {
      const option = document.createElement('option');
      option.value = make;
      option.textContent = make;
      makeSelect.append(option);
    }
  } catch (error) {
    showResult(error.message, true);
  }
}

function showResult(message, isError = false, title = '') {
  quoteResult.hidden = false;
  quoteResult.classList.toggle('error', isError);
  quoteResult.replaceChildren();
  if (title) {
    const heading = document.createElement('strong');
    heading.textContent = title;
    quoteResult.append(heading);
  }
  quoteResult.append(document.createTextNode(message));
}

function updateModels() {
  const selectedMake = makeSelect.value;
  const specification = modelsByMake.find(item => item.make === selectedMake);
  modelSelect.replaceChildren(new Option('Choose model', ''));
  for (const model of specification?.models ?? []) {
    modelSelect.append(new Option(model, model));
  }
  modelSelect.disabled = !selectedMake;
}

function formatDate(value) {
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? '' : new Intl.DateTimeFormat(undefined, { dateStyle: 'medium' }).format(date);
}

async function loadQuoteHistory() {
  historyList.replaceChildren();
  const loading = document.createElement('p');
  loading.className = 'empty-history';
  loading.textContent = 'Loading recent quotes…';
  historyList.append(loading);

  try {
    const quotes = await readJson(await fetch(`/Quotes?count=${encodeURIComponent(historyCountSelect.value)}`));
    historyList.replaceChildren();
    if (!quotes.length) {
      const empty = document.createElement('p');
      empty.className = 'empty-history';
      empty.textContent = 'No quotes yet. Your first one could be just around the corner.';
      historyList.append(empty);
      return;
    }

    for (const quote of quotes) {
      const row = document.createElement('div');
      row.className = 'history-row';
      const car = document.createElement('span');
      car.className = 'history-car';
      car.textContent = `${quote.make} ${quote.model}`;
      const date = document.createElement('span');
      date.className = 'history-date';
      date.textContent = formatDate(quote.createdAtUtc);
      const type = document.createElement('span');
      type.className = 'history-type';
      type.textContent = formatInsuranceType(quote.insuranceType);
      const premium = document.createElement('span');
      premium.className = 'history-premium';
      premium.textContent = new Intl.NumberFormat(undefined, { style: 'currency', currency: 'GBP' }).format(quote.premium);
      row.append(car, date, type, premium);
      historyList.append(row);
    }
  } catch (error) {
    historyList.replaceChildren();
    const failure = document.createElement('p');
    failure.className = 'empty-history';
    failure.textContent = error.message;
    historyList.append(failure);
  }
}

makeSelect.addEventListener('change', updateModels);
historyCountSelect.addEventListener('change', loadQuoteHistory);

quoteForm.addEventListener('submit', async event => {
  event.preventDefault();
  if (!quoteForm.reportValidity()) return;

  submitButton.disabled = true;
  submitButton.querySelector('span:first-child').textContent = 'Finding your quote…';
  quoteResult.hidden = true;

  const request = {
    dateOfBirth: dateOfBirthInput.value,
    make: makeSelect.value,
    model: modelSelect.value,
    insuranceType: insuranceTypeSelect.value
  };

  try {
    const result = await readJson(await fetch('/Quote', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(request)
    }));

    if (!result.quoteAvailable) {
      showResult(result.errorMessage || 'A quote is not available for those details.', true, 'No quote available');
      return;
    }

    const premium = new Intl.NumberFormat(undefined, { style: 'currency', currency: 'GBP' }).format(result.quote);
    showResult(`Your ${request.make} ${request.model} quote is ${premium}.`, false, 'Your quote is ready!');
    await loadQuoteHistory();
  } catch (error) {
    showResult(error.message, true, 'We couldn’t get your quote');
  } finally {
    submitButton.disabled = false;
    submitButton.querySelector('span:first-child').textContent = 'Get my quote';
  }
});

loadQuoteOptions();
loadQuoteHistory();
