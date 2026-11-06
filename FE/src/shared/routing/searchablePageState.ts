export type SearchablePageState<TSearchField extends string> = {
  page: number
  pageSize: number
  searchField: TSearchField
  searchValue: string
}

export function readSearchablePageState<TSearchField extends string>(
  defaults: SearchablePageState<TSearchField>,
  supportedFields: readonly TSearchField[],
  maxPageSize: number,
): SearchablePageState<TSearchField> {
  const params = new URLSearchParams(window.location.search)
  const requestedPage = Number(params.get('page'))
  const requestedPageSize = Number(params.get('pageSize'))
  const requestedSearchField = params.get('searchField')

  return {
    page: Number.isInteger(requestedPage) && requestedPage > 0 ? requestedPage : defaults.page,
    pageSize: Number.isInteger(requestedPageSize)
      && requestedPageSize > 0
      && requestedPageSize <= maxPageSize
      ? requestedPageSize
      : defaults.pageSize,
    searchField: supportedFields.find((field) => field === requestedSearchField) ?? defaults.searchField,
    searchValue: params.get('searchValue') ?? defaults.searchValue,
  }
}

export function writeSearchablePageState<TSearchField extends string>(
  state: SearchablePageState<TSearchField>,
  defaults: SearchablePageState<TSearchField>,
) {
  const params = new URLSearchParams()

  if (state.page !== defaults.page) {
    params.set('page', String(state.page))
  }

  if (state.pageSize !== defaults.pageSize) {
    params.set('pageSize', String(state.pageSize))
  }

  if (state.searchValue.trim()) {
    params.set('searchField', state.searchField)
    params.set('searchValue', state.searchValue.trim())
  }

  const queryString = params.toString()

  window.history.replaceState(
    window.history.state,
    '',
    `${window.location.pathname}${queryString ? `?${queryString}` : ''}${window.location.hash}`,
  )
}
